using Deprem.Application;
using Deprem.Domain;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Deprem.Infrastructure;

public sealed class DepremSatiri
{
    public Guid Id { get; set; }
    public DateTimeOffset Zaman { get; set; }
    public string Saglayici { get; set; } = "";
    public string KaynakKimligi { get; set; } = "";
    public double Buyukluk { get; set; }
    public double DerinlikKm { get; set; }
    public string Bolge { get; set; } = "";
    public string BuyuklukTuru { get; set; } = "";
    public double Enlem { get; set; }
    public double Boylam { get; set; }
    public string Yer { get; set; } = "";
    public DateTimeOffset OlusZamani { get; set; }
    public void Ata(Olay o)
    {
        Saglayici = o.Saglayici.Trim(); KaynakKimligi = o.KaynakKimligi.Trim(); Buyukluk = o.Model.Buyukluk;
        DerinlikKm = o.Model.DerinlikKm; Bolge = o.Model.Bolge; BuyuklukTuru = o.BuyuklukTuru;
        Enlem = o.Enlem; Boylam = o.Boylam; Yer = o.Yer; OlusZamani = o.OlusZamani.ToUniversalTime();
    }
    public Kayit<Olay> Oku() => new(Id, Zaman, new(new(Buyukluk, DerinlikKm, Bolge), BuyuklukTuru, Enlem, Boylam, Yer, OlusZamani, Saglayici, KaynakKimligi));
}

public sealed class HesapSatiri
{
    public Guid Id { get; set; }
    public Guid DepremId { get; set; }
    public DateTimeOffset Zaman { get; set; }
    public string SonucJson { get; set; } = "";
    public Kayit<Hesap> Oku() => new(Id, Zaman, new(DepremId, JsonSerializer.Deserialize<Sonuc>(SonucJson)!));
}

public sealed class UyariSatiri
{
    public Guid Id { get; set; }
    public Guid DepremId { get; set; }
    public Guid HesapId { get; set; }
    public string Kural { get; set; } = "";
    public int Seviye { get; set; }
    public DateTimeOffset Zaman { get; set; }
    public DateTimeOffset? OnayZamani { get; set; }
    public Kayit<Uyari> Oku() => new(Id, Zaman, new(DepremId, HesapId, Kural, Seviye, OnayZamani));
}

public sealed class BinaSatiri
{
    public Guid Id { get; set; }
    public DateTimeOffset Zaman { get; set; }
    public string Envanter { get; set; } = "";
    public Kayit<Yapi> Oku() => new(Id, Zaman, JsonSerializer.Deserialize<Yapi>(Envanter)!);
}
public sealed class IncelemeSatiri
{
    public Guid Id { get; set; }
    public Guid BinaId { get; set; }
    public DateTimeOffset Zaman { get; set; }
    public string SonucJson { get; set; } = "";
    public Kayit<Inceleme> Oku() => new(Id, Zaman, JsonSerializer.Deserialize<Inceleme>(SonucJson)!);
}
public sealed class Iz
{
    public long Id { get; set; }
    public Guid KayitId { get; set; }
    public string Islem { get; set; } = "";
    public DateTimeOffset Zaman { get; set; }
}

public class Veritabani(DbContextOptions<Veritabani> options) : DbContext(options)
{
    public DbSet<DepremSatiri> Depremler => Set<DepremSatiri>();
    public DbSet<HesapSatiri> Hesaplar => Set<HesapSatiri>();
    public DbSet<UyariSatiri> Uyarilar => Set<UyariSatiri>();
    public DbSet<BinaSatiri> Binalar => Set<BinaSatiri>();
    public DbSet<IncelemeSatiri> Incelemeler => Set<IncelemeSatiri>();
    public DbSet<Iz> Izler => Set<Iz>();
    public void Kaydet(Guid id, string eylem) => Izler.Add(new() { KayitId = id, Islem = eylem, Zaman = DateTimeOffset.UtcNow });
    protected override void OnModelCreating(ModelBuilder b)
    {
        var d = b.Entity<DepremSatiri>();
        d.Property(x => x.Saglayici).HasMaxLength(80);
        d.Property(x => x.KaynakKimligi).HasMaxLength(100);
        d.HasIndex(x => new { x.Saglayici, x.KaynakKimligi }).IsUnique();
        d.HasIndex(x => x.OlusZamani);
        d.ToTable(t => t.HasCheckConstraint("CK_Deprem_Girdi", "[DerinlikKm] > 0 AND [DerinlikKm] <= 800 AND [Buyukluk] BETWEEN -2 AND 10 AND [Enlem] BETWEEN -90 AND 90 AND [Boylam] BETWEEN -180 AND 180"));
        b.Entity<HesapSatiri>().HasOne<DepremSatiri>().WithMany().HasForeignKey(x => x.DepremId);
        b.Entity<HesapSatiri>().HasAlternateKey(x => new { x.Id, x.DepremId });
        var u = b.Entity<UyariSatiri>();
        u.Property(x => x.Kural).HasMaxLength(200);
        u.HasOne<HesapSatiri>().WithMany().HasForeignKey(x => new { x.HesapId, x.DepremId }).HasPrincipalKey(x => new { x.Id, x.DepremId });
        u.HasIndex(x => new { x.DepremId, x.Kural }).IsUnique();
        b.Entity<IncelemeSatiri>().HasOne<BinaSatiri>().WithMany().HasForeignKey(x => x.BinaId);
        b.Entity<Iz>().HasIndex(x => x.KayitId);
    }
}
