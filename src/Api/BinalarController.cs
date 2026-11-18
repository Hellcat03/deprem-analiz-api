using System.Text.Json;
using Deprem.Application;
using Deprem.Infrastructure;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
[ApiController, Route("api/v1/binalar"), Yonetim]
public class BinalarController(Veritabani db, YapiKontrol kontrol) : ControllerBase
{
    [HttpGet("belge-turleri")] public string[] Belgeler() => YapiInceleme.Belgeler;
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Kayit<Yapi>>> Oku(Guid id, CancellationToken ct) =>
        await db.Binalar.FindAsync([id], ct) is { } b ? b.Oku() : NotFound();
    [HttpPost, ProducesResponseType<Kayit<Yapi>>(201)]
    public async Task<ActionResult<Kayit<Yapi>>> Ekle(Yapi y, CancellationToken ct)
    {
        await kontrol.ValidateAndThrowAsync(y, ct);
        var b = new BinaSatiri { Id = Guid.NewGuid(), Zaman = DateTimeOffset.UtcNow, Envanter = JsonSerializer.Serialize(y) };
        db.Binalar.Add(b); db.Kaydet(b.Id, "Bina envanteri kaydedildi"); await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Oku), new { id = b.Id }, b.Oku());
    }
    [HttpPost("{id:guid}/degerlendirmeler"), ProducesResponseType<Kayit<Inceleme>>(201)]
    public async Task<ActionResult<Kayit<Inceleme>>> Degerlendir(Guid id, CancellationToken ct)
    {
        if (await db.Binalar.FindAsync([id], ct) is not { } b) return NotFound();
        var s = new IncelemeSatiri
        {
            Id = Guid.NewGuid(),
            BinaId = id,
            Zaman = DateTimeOffset.UtcNow,
            SonucJson = JsonSerializer.Serialize(YapiInceleme.Incele(b.Oku().Veri))
        };
        db.Incelemeler.Add(s); db.Kaydet(s.Id, "Belge yeterliliği incelendi"); await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(IncelemeOku), new { id, incelemeId = s.Id }, s.Oku());
    }
    [HttpGet("{id:guid}/degerlendirmeler/{incelemeId:guid}")]
    public async Task<ActionResult<Kayit<Inceleme>>> IncelemeOku(Guid id, Guid incelemeId, CancellationToken ct) =>
        await db.Incelemeler.SingleOrDefaultAsync(x => x.Id == incelemeId && x.BinaId == id, ct) is { } s ? s.Oku() : NotFound();
}
