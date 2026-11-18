using Deprem.Domain;
using Deprem.Application;
namespace Deprem.Tests;
public class ModelTests
{
    static Bolge Bolge => new("TR",0.5,0.5,"test",[
        new(null,4,"a","",0,""),new(4,6,"b","",1,""),new(6,8,"c","",2,""),new(8,null,"d","",3,"")],2);
    [Theory]
    [InlineData(3.999,0)][InlineData(4,1)][InlineData(5.999,1)][InlineData(6,2)][InlineData(7.999,2)][InlineData(8,3)]
    public void Sinir(double tahmin,int seviye)
    {
        var s = Model.Hesapla(tahmin/2+1.5,10,Bolge);
        Assert.Equal(tahmin,s.Tahmin,10);
        Assert.Equal(1,s.LogDerinlik);
        Assert.Equal(seviye,s.Sinif.Seviye);
        Assert.Equal(seviye>=2,Model.UyariGerekir(s,Bolge));
    }
    [Theory][InlineData(11,10)][InlineData(4,0)][InlineData(4,-1)][InlineData(double.NaN,10)][InlineData(4,801)]
    public void Gecersiz(double m,double h) => Assert.Throws<ArgumentException>(()=>Model.Hesapla(m,h,Bolge));
    [Fact] public void Katsayi() => Assert.Equal(8.86,Model.Hesapla(6,10,Bolge with { Katsayi=.57 }).Tahmin,10);
    [Fact] public void Bolen() => Assert.Throws<ArgumentException>(()=>Model.Hesapla(5,10,Bolge with { Bolen=0 }));
    [Fact] public void BetonTekBasinaYetmez() => Assert.Equal("Yetersiz veri",YapiInceleme.Incele(new("Test","Sentetik","C80",2000,5,null,[],null)).Durum);
}
