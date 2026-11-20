using System.Text.Json.Serialization;

using MenuTranslation.Api.Endpoints;
using MenuTranslation.Application;
using MenuTranslation.Infrastructure;
using MenuTranslation.Infrastructure.Persistence;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication(builder.Configuration)
    .AddInfrastructure(builder.Configuration);
builder.Services.AddProblemDetails();
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

WebApplication app = builder.Build();

// Örnek proje migration yerine şemayı başlangıçta oluşturur; gerçek projede EF Core migration'ları kullanılır.
if (app.Configuration.GetValue<bool>("Database:EnsureCreated"))
{
    await using AsyncServiceScope scope = app.Services.CreateAsyncScope();
    AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
    await DemoData.SeedAsync(db, scope.ServiceProvider.GetRequiredService<TimeProvider>());
}

app.UseExceptionHandler();
app.MapMenuEndpoints();

await app.RunAsync();

/// <summary>Entegrasyon testlerinin <c>WebApplicationFactory</c> ile uygulamayı başlatabilmesi için.</summary>
public partial class Program;
