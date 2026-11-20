using MenuTranslation.Domain.Menus;
using MenuTranslation.Infrastructure.Persistence;
using MenuTranslation.Infrastructure.Translation;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace MenuTranslation.IntegrationTests;

/// <summary>
/// Uygulamayı gerçek PostgreSQL ve Redis kapsayıcılarıyla başlatır (Docker gerekir). Çeviri servisi sahte
/// çeviricidir; testler onu kapatabilir, yavaşlatabilir ve hangi metinlerin gönderildiğine bakabilir.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    /// <summary>Testlerdeki çeviri süre sınırı: sahte çevirici bundan uzun beklerse menü orijinal diliyle döner.</summary>
    public const int TimeoutMilliseconds = 500;

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine").Build();
    private readonly RedisContainer _redis = new RedisBuilder("redis:7-alpine").Build();

    public FakeTranslationService Translator => Services.GetRequiredService<FakeTranslationService>();

    public async Task InitializeAsync() => await Task.WhenAll(_postgres.StartAsync(), _redis.StartAsync());

    async Task IAsyncLifetime.DisposeAsync()
    {
        await DisposeAsync();
        await _postgres.DisposeAsync();
        await _redis.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Postgres", _postgres.GetConnectionString());
        builder.UseSetting("ConnectionStrings:Redis", _redis.GetConnectionString());
        builder.UseSetting("Database:EnsureCreated", "true");
        builder.UseSetting("Translation:TimeoutMilliseconds", TimeoutMilliseconds.ToString());
    }

    /// <summary>Her test kendi menüsüyle çalışır; böylece testler birbirinin önbelleğini ve çevirilerini görmez.</summary>
    public async Task<TestMenu> CreateMenuAsync()
    {
        await using AsyncServiceScope scope = Services.CreateAsyncScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        DateTimeOffset createdAt = DateTimeOffset.UtcNow.AddMinutes(-5);
        Menu menu = new()
        {
            Id = Guid.CreateVersion7(),
            Name = "Test kafe",
            SourceLanguage = "tr",
            CityId = DemoData.YozgatCityId,
            Items =
            [
                new MenuItem { Id = Guid.CreateVersion7(), Name = "Arabaşı çorbası", Description = "Yozgat usulü acılı tavuk çorbası", PriceCents = 16000, SortOrder = 1, TextUpdatedAt = createdAt },
                new MenuItem { Id = Guid.CreateVersion7(), Name = "Türk kahvesi", Description = "Közde pişmiş", PriceCents = 9000, SortOrder = 2, TextUpdatedAt = createdAt },
                new MenuItem { Id = Guid.CreateVersion7(), Name = "Fıstıklı baklava", PriceCents = 18000, SortOrder = 3, TextUpdatedAt = createdAt }
            ]
        };
        db.Menus.Add(menu);
        await db.SaveChangesAsync();

        return new TestMenu(menu.Id, menu.Items[0].Id, menu.Items[1].Id, menu.Items[2].Id);
    }
}

public sealed record TestMenu(Guid Id, Guid SoupId, Guid CoffeeId, Guid BaklavaId);
