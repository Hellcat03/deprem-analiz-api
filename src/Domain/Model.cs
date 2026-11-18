namespace Deprem.Domain;

public record Aralik(double? Alt, double? Ust, string Ad, string Aciklama, int Seviye, string Oneri)
{
    public bool Kapsar(double deger) => (Alt is null || deger >= Alt) && (Ust is null || deger < Ust);
}

public record Bolge(string Kod, double Katsayi, double Bolen, string Surum, Aralik[] Siniflar, int? UyariEsigi);
public record Sonuc(double Buyukluk, double DerinlikKm, double LogDerinlik, double Katsayi,
    double Bolen, double Pay, double Tahmin, string Surum, string Bolge, Aralik Sinif, string Durum);

public static class Model
{
    public const string Durum = "Deneysel proje tahmini";
    public static void Dogrula(Bolge b)
    {
        if (string.IsNullOrWhiteSpace(b.Kod) || string.IsNullOrWhiteSpace(b.Surum) ||
            !double.IsFinite(b.Katsayi) || !double.IsFinite(b.Bolen) || b.Bolen <= 0 ||
            b.Siniflar.Length == 0 || b.Siniflar[0].Alt != null || b.Siniflar[^1].Ust != null)
            throw new ArgumentException("Model yapılandırması geçersiz.");
        for (int i = 0; i < b.Siniflar.Length; i++)
        {
            var s = b.Siniflar[i];
            if ((i > 0 && (s.Alt == null || s.Alt != b.Siniflar[i - 1].Ust)) ||
                s.Alt >= s.Ust || s.Alt is double a && !double.IsFinite(a) ||
                s.Ust is double u && !double.IsFinite(u))
                throw new ArgumentException("Sınıf aralıkları kesintisiz ve sıralı olmalıdır.");
        }
    }

    public static Sonuc Hesapla(double m, double h, Bolge b)
    {
        Dogrula(b);
        if (!double.IsFinite(m) || m < -2 || m > 10 || !double.IsFinite(h) || h <= 0 || h > 800)
            throw new ArgumentException("Proje girdi sınırları: büyüklük −2 ile 10, derinlik 0'dan büyük ve en fazla 800 km.");
        double log = Math.Log10(h), pay = m - (log + b.Katsayi), tahmin = pay / b.Bolen;
        if (!double.IsFinite(tahmin)) throw new ArgumentException("Sonuç sayı aralığını aşıyor.");
        return new(m, h, log, b.Katsayi, b.Bolen, pay, tahmin, b.Surum, b.Kod, b.Siniflar.Single(s => s.Kapsar(tahmin)), Durum);
    }

    public static bool UyariGerekir(Sonuc s, Bolge b) => b.UyariEsigi is int esik && s.Sinif.Seviye >= esik;
}
