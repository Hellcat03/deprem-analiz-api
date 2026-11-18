using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;
using Deprem.Domain;
using Deprem.Infrastructure;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Serilog;

var b = WebApplication.CreateBuilder(args);
var tr = CultureInfo.GetCultureInfo("tr-TR");
CultureInfo.DefaultThreadCurrentCulture = tr;
CultureInfo.DefaultThreadCurrentUICulture = tr;
ValidatorOptions.Global.LanguageManager.Culture = tr;
b.Host.UseSerilog((ctx, log) => log.MinimumLevel.Information().MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning).WriteTo.Console());
b.Services.AddControllers().AddJsonOptions(o => o.JsonSerializerOptions.RespectRequiredConstructorParameters = true)
    .ConfigureApiBehaviorOptions(o => o.InvalidModelStateResponseFactory = context => new BadRequestObjectResult(
        new ProblemDetails { Status = 400, Title = "İstek biçimi veya zorunlu alanlar geçersiz." }));
b.Services.AddEndpointsApiExplorer();
b.Services.AddSwaggerGen(o => o.SwaggerDoc("v1", new()
{
    Title = "Deprem Analiz API",
    Version = "v1",
    Description = "Akademik deneysel model. Korunan işlemler güvenilir sunucudan X-Yonetim-Anahtari başlığı ile çağrılır."
}));
b.Services.AddDbContext<Veritabani>(o => o.UseSqlServer(b.Configuration.GetConnectionString("Deprem")
    ?? throw new InvalidOperationException("ConnectionStrings__Deprem ayarlanmalıdır.")));
using var model = File.OpenRead(Path.Combine(b.Environment.ContentRootPath, b.Configuration["ModelDosyasi"] ?? "model.json"));
var bolgeler = JsonSerializer.Deserialize<Dictionary<string, Bolge[]>>(model)!["Bolgeler"];
foreach (var bolge in bolgeler) Model.Dogrula(bolge);
if (bolgeler.Length == 0 || bolgeler.DistinctBy(x => x.Kod).Count() != bolgeler.Length) throw new InvalidDataException("Bölge kodları geçersiz.");
b.Services.AddSingleton(new Katalog(bolgeler));
b.Services.AddSingleton<OlayKontrol>();
b.Services.AddSingleton<YapiKontrol>();
b.Services.AddCors(o => o.AddDefaultPolicy(p => p.WithOrigins(b.Configuration.GetSection("IzinliKokenler").Get<string[]>() ?? []).AllowAnyHeader().AllowAnyMethod()));
b.Services.AddRateLimiter(o =>
{
    o.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(http => RateLimitPartition.GetFixedWindowLimiter(
        http.Connection.RemoteIpAddress?.ToString() ?? "yerel", _ => new() { PermitLimit = 180, Window = TimeSpan.FromMinutes(1) }));
    o.OnRejected = async (ctx, ct) => await Results.Problem(statusCode: 429, title: "İstek sınırı aşıldı.").ExecuteAsync(ctx.HttpContext);
});
var app = b.Build();
app.UseExceptionHandler(handler => handler.Run(async http =>
{
    var e = http.Features.Get<IExceptionHandlerFeature>()?.Error;
    var (status, title) = e switch
    {
        ValidationException v => (400, string.Join(" ", v.Errors.Select(s => s.ErrorMessage))),
        ArgumentException a => (400, a.Message),
        DbUpdateException { InnerException: SqlException { Number: 2601 or 2627 } } => (409, "Olay veya uyarı zaten kayıtlı."),
        SqlException { Number: 1205 } => (409, "Eşzamanlı değişiklik oldu. İsteği yeniden gönderin."),
        _ => (500, "İşlem tamamlanamadı.")
    };
    await Results.Problem(statusCode: status, title: title).ExecuteAsync(http);
}));
app.UseStatusCodePages(async ctx => await Results.Problem(statusCode: ctx.HttpContext.Response.StatusCode, title: "İstek tamamlanamadı.").ExecuteAsync(ctx.HttpContext));
app.UseSerilogRequestLogging();
app.UseCors();
app.UseRateLimiter();
app.Use(async (ctx, next) =>
{
    if (ctx.GetEndpoint()?.Metadata.GetMetadata<YonetimAttribute>() != null)
    {
        var beklenen = b.Configuration["YonetimAnahtari"];
        var verilen = ctx.Request.Headers["X-Yonetim-Anahtari"].ToString();
        if (string.IsNullOrEmpty(beklenen) || string.IsNullOrEmpty(verilen) || !CryptographicOperations.FixedTimeEquals(
            SHA256.HashData(Encoding.UTF8.GetBytes(beklenen)), SHA256.HashData(Encoding.UTF8.GetBytes(verilen))))
        {
            await Results.Problem(statusCode: 401, title: "Yönetim yetkisi gerekiyor.").ExecuteAsync(ctx); return;
        }
    }
    await next();
});
app.UseSwagger();
app.UseSwaggerUI(o => o.SwaggerEndpoint("/swagger/v1/swagger.json", "Türkçe API v1"));
app.MapControllers();
app.MapGet("/saglik", async (Veritabani db, CancellationToken ct) =>
    await db.Database.CanConnectAsync(ct) ? Results.Ok(new { durum = "Hazır" }) : Results.Problem(statusCode: 503, title: "Veritabanına erişilemiyor."));
if (args.Contains("--goc"))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<Veritabani>().Database.MigrateAsync();
    return;
}
app.Run();
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)] public sealed class YonetimAttribute : Attribute { }
public partial class Program { }

