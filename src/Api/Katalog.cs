using Deprem.Application;
using Deprem.Domain;
public class Katalog(Bolge[] bolgeler)
{
    public IReadOnlyList<Bolge> Bolgeler => bolgeler;
    public Bolge Bul(string kod) => bolgeler.FirstOrDefault(x=>x.Kod==kod) ?? throw new ArgumentException("Bölge kodu tanınmadı.");
    public Sonuc Hesapla(Girdi g) => Model.Hesapla(g.Buyukluk,g.DerinlikKm,Bul(g.Bolge));
}
