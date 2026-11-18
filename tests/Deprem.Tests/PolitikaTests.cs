using Deprem.Domain;
using Deprem.Application;
namespace Deprem.Tests;

public class PolitikaTests
{
    static Bolge B => new("TR", .57, .5, "test", [new(null, 4, "Alt", "", 0, ""), new(4, null, "Üst", "", 1, "")], 1);
    [Fact]
    public void EsikKapatilabilir()
    {
        var b = B with { UyariEsigi = null };
        Assert.False(Model.UyariGerekir(Model.Hesapla(9, 1, b), b));
    }
    [Fact]
    public void BolgeselKatsayiEtkiEder()
    {
        var s = Model.Hesapla(4, 10, B);
        var diger = Model.Hesapla(4, 10, B with { Kod = "SENTETIK", Katsayi = 1.57 });
        Assert.Equal(2, s.Tahmin - diger.Tahmin, 10);
    }
    [Fact]
    public void AralikBosluguReddedilir()
    {
        var b = B with { Siniflar = [B.Siniflar[0], B.Siniflar[1] with { Alt = 5 }] };
        Assert.Throws<ArgumentException>(() => Model.Dogrula(b));
    }
    [Theory]
    [InlineData(-2, 800)]
    [InlineData(10, .000001)]
    public void UclarKirpilmaz(double m, double h)
    {
        Assert.Equal((m - Math.Log10(h) - .57) / .5, Model.Hesapla(m, h, B).Tahmin, 10);
    }
    [Fact]
    public void TamEnvanterUzmanGerektirir()
    {
        var sonuc = YapiInceleme.Incele(new("Sentetik", "Test", null, null, null, null, YapiInceleme.Belgeler, null));
        Assert.Empty(sonuc.BildirilmeyenBelgeler);
        Assert.Equal("Uzman incelemesi gerekli", sonuc.Durum);
    }
}
