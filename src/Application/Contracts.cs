using Deprem.Domain;

namespace Deprem.Application;

public record Girdi(double Buyukluk, double DerinlikKm, string Bolge);
public record Olay(Girdi Model, string BuyuklukTuru, double Enlem, double Boylam, string Yer,
    DateTimeOffset OlusZamani, string Saglayici, string KaynakKimligi);
public record Yapi(string Ad, string Konum, string? BetonSinifi, int? YapimYili, int? KatSayisi,
    string? TasiyiciSistem, string[] Belgeler, string? Yonetmelik);
public record Inceleme(string Durum, string[] BildirilmeyenBelgeler, string Dayanak);
public static class YapiInceleme
{
    public static readonly string[] Belgeler = ["Taşıyıcı sistem rölevesi", "Donatı tespiti", "Malzeme deneyleri", "Zemin etüdü", "Yük bilgileri", "Hasar ve tadilat kayıtları"];
    public static Inceleme Incele(Yapi y)
    {
        var eksik = Belgeler.Except(y.Belgeler).ToArray();
        return new(eksik.Length == 0 ? "Uzman incelemesi gerekli" : "Yetersiz veri", eksik,
            "TBDY 2018 Bölüm 15 kapsamında belge envanteri. Belgelerin içeriği ve bina performansı değerlendirilmemiştir.");
    }
}
public record Kayit<T>(Guid Id, DateTimeOffset Zaman, T Veri);
public record Hesap(Guid DepremId, Sonuc Sonuc);
public record Uyari(Guid DepremId, Guid HesapId, string Kural, int Seviye, DateTimeOffset? OnayZamani);
public record Islem(Guid DepremId, Guid HesapId, Guid? UyariId, Sonuc Sonuc);
public interface IOlaySaglayici
{
    string Ad { get; }
    IAsyncEnumerable<Olay> Oku(DateTimeOffset baslangic, CancellationToken iptal);
}

public record BolgeBilgisi(string Kod, string Surum, double Katsayi, double Bolen, int? UyariEsigi, string Durum);
