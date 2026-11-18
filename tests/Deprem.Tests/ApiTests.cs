using System.Net;
using System.Net.Http.Json;
using Deprem.Application;
using Deprem.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Deprem.Tests;
public sealed class Sunucu : WebApplicationFactory<Program>, IAsyncLifetime
{
    public string Anahtar { get; }=Guid.NewGuid().ToString("N");
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var sql=new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("TEST_SQL")
            ?? @"Server=(localdb)\MSSQLLocalDB;Integrated Security=true;TrustServerCertificate=true");
        sql.InitialCatalog="DepremTest_"+Guid.NewGuid().ToString("N");
        builder.ConfigureAppConfiguration((_,c)=>c.AddInMemoryCollection(new Dictionary<string,string?>{
            ["ConnectionStrings:Deprem"]=sql.ConnectionString,["YonetimAnahtari"]=Anahtar}));
    }
    public async Task InitializeAsync(){
        using var s=Services.CreateScope();
        await s.ServiceProvider.GetRequiredService<Veritabani>().Database.MigrateAsync();
    }
    async Task IAsyncLifetime.DisposeAsync(){
        using var s=Services.CreateScope();
        await s.ServiceProvider.GetRequiredService<Veritabani>().Database.EnsureDeletedAsync();
        await DisposeAsync();
    }
}
public class ApiTests(Sunucu app):IClassFixture<Sunucu>
{
    const string Api="/api/v1";
    private HttpClient Yonetim(){
        var c=app.CreateClient(); c.DefaultRequestHeaders.Add("X-Yonetim-Anahtari",app.Anahtar);return c;
    }
    [Fact] public async Task SentetikOlayAkisi()
    {
        using var c=Yonetim();
        var o=new Olay(new(6,10,"TR"),"Mw",39,32,"Sentetik saha",DateTimeOffset.UtcNow,"Sentetik","test-1");
        var ekle=await c.PostAsJsonAsync(Api+"/depremler",o);
        Assert.Equal(HttpStatusCode.Created,ekle.StatusCode);
        var r=(await ekle.Content.ReadFromJsonAsync<Islem>())!;
        Assert.NotNull(r.UyariId);
        Assert.Equal(8.86,r.Sonuc.Tahmin,10);
        var ayni=await c.PostAsJsonAsync(Api+"/depremler",o);
        Assert.Equal(HttpStatusCode.Conflict,ayni.StatusCode);
        var yol=Api+"/depremler/"+r.DepremId;
        var tekrar=await c.PutAsJsonAsync(yol,o);
        Assert.Equal(r.UyariId,(await tekrar.Content.ReadFromJsonAsync<Islem>())!.UyariId);
        Assert.Equal(2,(await c.GetFromJsonAsync<Kayit<Hesap>[]>(yol+"/hesaplamalar"))!.Length);
        var uyari=Api+"/uyarilar/"+r.UyariId+"/onay";
        var ilk=await c.PostAsync(uyari,null);
        var ikinci=await c.PostAsync(uyari,null);
        Assert.Equal((await ilk.Content.ReadFromJsonAsync<Kayit<Uyari>>())!.Veri.OnayZamani,
            (await ikinci.Content.ReadFromJsonAsync<Kayit<Uyari>>())!.Veri.OnayZamani);
    }
    [Fact] public async Task BinaAkisi(){
        using var c=Yonetim();
        var y=new Yapi("Sentetik","Kurgusal saha","C30",2000,4,"Çerçeve",[],null);
        Assert.Equal(HttpStatusCode.BadRequest,(await c.PostAsJsonAsync(Api+"/binalar",y with {KatSayisi=0})).StatusCode);
        var ekle=await c.PostAsJsonAsync(Api+"/binalar",y);
        var kayit=(await ekle.Content.ReadFromJsonAsync<Kayit<Yapi>>())!;
        var rapor=await c.PostAsync(Api+"/binalar/"+kayit.Id+"/degerlendirmeler",null);
        Assert.Equal("Yetersiz veri",(await rapor.Content.ReadFromJsonAsync<Kayit<Inceleme>>())!.Veri.Durum);
    }
    [Fact] public async Task Hatalar(){
        using var c=app.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized,(await c.PostAsJsonAsync(Api+"/depremler",new{})).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,(await c.PostAsJsonAsync(Api+"/hesaplamalar/onizleme",new{derinlikKm=10,bolge="TR"})).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,(await c.GetAsync(Api+"/depremler?adet=0")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await c.GetAsync(Api+"/depremler/"+Guid.Empty)).StatusCode);
        Assert.Equal(HttpStatusCode.OK,(await c.GetAsync("/swagger/v1/swagger.json")).StatusCode);
    }
}
