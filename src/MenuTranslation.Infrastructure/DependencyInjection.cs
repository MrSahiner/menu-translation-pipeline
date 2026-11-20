using MenuTranslation.Application.Abstractions;
using MenuTranslation.Infrastructure.Caching;
using MenuTranslation.Infrastructure.Persistence;
using MenuTranslation.Infrastructure.Translation;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MenuTranslation.Infrastructure;

/// <summary>Veritabanı, önbellek ve çeviri servisinin kayıtları.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        string postgres = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("ConnectionStrings:Postgres tanımlı değil.");
        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(postgres));
        services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());

        // Redis tanımlıysa HybridCache onu ikinci seviye olarak kullanır; tanımlı değilse yalnızca bellek önbelleği.
        if (configuration.GetConnectionString("Redis") is { Length: > 0 } redis)
        {
            services.AddStackExchangeRedisCache(options => options.Configuration = redis);
        }

        services.AddHybridCache();
        services.Configure<CachingOptions>(configuration.GetSection(CachingOptions.SectionName));
        services.AddSingleton<IMenuCache, HybridMenuCache>();

        // Gerçek üründe burada DeepL istemcisi kaydedilir.
        services.AddSingleton<FakeTranslationService>();
        services.AddSingleton<ITranslationService>(provider => provider.GetRequiredService<FakeTranslationService>());

        return services;
    }
}
