using System.Data;
using System.Text.Json;
using Deprem.Application;
using Deprem.Domain;
using Deprem.Infrastructure;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController, Route("api/v1")]
public class DepremlerController(Veritabani db, Katalog katalog, OlayKontrol kontrol) : ControllerBase
{
    [HttpPost("hesaplamalar/onizleme")]
    public Sonuc Onizleme(Girdi g) => katalog.Hesapla(g);

    [HttpGet("bolgeler")]
    public IEnumerable<BolgeBilgisi> Bolgeler() => katalog.Bolgeler.Select(b => new BolgeBilgisi(b.Kod, b.Surum, b.Katsayi, b.Bolen, b.UyariEsigi, Model.Durum));
    [HttpGet("bolgeler/{kod}/siniflar")]
    public IEnumerable<Aralik> Siniflar(string kod) => katalog.Bul(kod).Siniflar;

    [HttpGet("depremler")]
    public async Task<IEnumerable<Kayit<Olay>>> Liste(CancellationToken ct, int sayfa = 1, int adet = 25, string? saglayici = null, bool artan = false)
    {
        var q = db.Depremler.AsNoTracking();
        if (saglayici != null) q = q.Where(x => x.Saglayici == saglayici);
        var s = artan ? q.OrderBy(x => x.OlusZamani).ThenBy(x => x.Id) : q.OrderByDescending(x => x.OlusZamani).ThenBy(x => x.Id);
        return (await s.Skip(Atla(sayfa, adet)).Take(adet).ToListAsync(ct)).Select(x => x.Oku());
    }
    public static int Atla(int sayfa, int adet) => sayfa is < 1 or > 1000000 || adet is < 1 or > 100
        ? throw new ArgumentException("Sayfa 1–1000000, adet 1–100 aralığında olmalıdır.") : (sayfa - 1) * adet;

    [HttpGet("depremler/{id:guid}")]
    public async Task<ActionResult<Kayit<Olay>>> Oku(Guid id, CancellationToken ct) =>
        await db.Depremler.FindAsync([id], ct) is { } d ? d.Oku() : NotFound();

    [HttpPost("depremler"), Yonetim, ProducesResponseType<Islem>(201)]
    public async Task<ActionResult<Islem>> Ekle(Olay o, CancellationToken ct)
    {
        var sonuc = await Yaz(null, o, ct);
        return CreatedAtAction(nameof(Oku), new { id = sonuc.DepremId }, sonuc);
    }
    [HttpPut("depremler/{id:guid}"), Yonetim]
    public async Task<ActionResult<Islem>> Guncelle(Guid id, Olay o, CancellationToken ct)
    {
        if (!await db.Depremler.AnyAsync(x => x.Id == id, ct)) return NotFound();
        return await Yaz(id, o, ct);
    }
    private async Task<Islem> Yaz(Guid? id, Olay o, CancellationToken ct)
    {
        await kontrol.ValidateAndThrowAsync(o, ct);
        var s = katalog.Hesapla(o.Model);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var zaman = DateTimeOffset.UtcNow;
        var d = id.HasValue ? await db.Depremler.SingleAsync(x => x.Id == id, ct) : new DepremSatiri { Id = Guid.NewGuid(), Zaman = zaman };
        d.Ata(o);
        if (!id.HasValue) db.Depremler.Add(d);
        var h = new HesapSatiri { Id = Guid.NewGuid(), DepremId = d.Id, Zaman = zaman, SonucJson = JsonSerializer.Serialize(s) };
        db.Hesaplar.Add(h);
        Guid? uyariId = null;
        var bolge = katalog.Bul(s.Bolge);
        if (Model.UyariGerekir(s, bolge))
        {
            var kural = $"{s.Bolge}/{s.Surum}/eşik-{bolge.UyariEsigi}/seviye-{s.Sinif.Seviye}";
            uyariId = await db.Uyarilar.Where(x => x.DepremId == d.Id && x.Kural == kural).Select(x => (Guid?)x.Id).SingleOrDefaultAsync(ct);
            if (uyariId == null)
            {
                var u = new UyariSatiri { Id = Guid.NewGuid(), DepremId = d.Id, HesapId = h.Id, Kural = kural, Seviye = s.Sinif.Seviye, Zaman = zaman };
                db.Uyarilar.Add(u); uyariId = u.Id; db.Kaydet(u.Id, "Proje uyarısı oluştu");
            }
        }
        db.Kaydet(h.Id, id.HasValue ? "Olay güncellendi ve yeniden hesaplandı" : "Olay alındı ve hesaplandı");
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return new(d.Id, h.Id, uyariId, s);
    }
    [HttpGet("hesaplamalar/{id:guid}")]
    public async Task<ActionResult<Kayit<Hesap>>> HesapOku(Guid id, CancellationToken ct) =>
        await db.Hesaplar.FindAsync([id], ct) is { } h ? h.Oku() : NotFound();
    [HttpGet("depremler/{id:guid}/hesaplamalar")]
    public async Task<ActionResult<IEnumerable<Kayit<Hesap>>>> Gecmis(Guid id, CancellationToken ct, int sayfa = 1, int adet = 25)
    {
        if (!await db.Depremler.AnyAsync(x => x.Id == id, ct)) return NotFound();
        return (await db.Hesaplar.Where(x => x.DepremId == id).OrderByDescending(x => x.Zaman).ThenBy(x => x.Id)
            .Skip(Atla(sayfa, adet)).Take(adet).ToListAsync(ct)).Select(x => x.Oku()).ToList();
    }
    [HttpGet("uyarilar")]
    public async Task<IEnumerable<Kayit<Uyari>>> Uyarilar(CancellationToken ct, int sayfa = 1, int adet = 25, bool? onaylandi = null)
    {
        var q = db.Uyarilar.AsNoTracking();
        if (onaylandi.HasValue) q = q.Where(x => (x.OnayZamani != null) == onaylandi.Value);
        return (await q.OrderByDescending(x => x.Zaman).ThenBy(x => x.Id).Skip(Atla(sayfa, adet)).Take(adet).ToListAsync(ct)).Select(x => x.Oku());
    }
    [HttpGet("uyarilar/{id:guid}")]
    public async Task<ActionResult<Kayit<Uyari>>> UyariOku(Guid id, CancellationToken ct) =>
        await db.Uyarilar.FindAsync([id], ct) is { } u ? u.Oku() : NotFound();
    [HttpPost("uyarilar/{id:guid}/onay"), Yonetim]
    public async Task<ActionResult<Kayit<Uyari>>> Onay(Guid id, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var t = DateTimeOffset.UtcNow;
        if (await db.Uyarilar.Where(x => x.Id == id && x.OnayZamani == null).ExecuteUpdateAsync(s => s.SetProperty(x => x.OnayZamani, t), ct) > 0)
        {
            db.Kaydet(id, "Proje uyarısı onaylandı"); await db.SaveChangesAsync(ct);
        }
        await tx.CommitAsync(ct);
        return await UyariOku(id, ct);
    }
}
