using MenuTranslation.Application.Abstractions;
using MenuTranslation.Application.Menus.Queries.GetTranslatedMenu;

using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;

namespace MenuTranslation.Infrastructure.Caching;

/// <summary>Menü önbelleği ayarları (<c>Caching</c> bölümü).</summary>
public sealed class CachingOptions
{
    public const string SectionName = "Caching";

    /// <summary>Menünün dağıtık önbellekte (Redis) kalma süresi.</summary>
    public int MenuMinutes { get; init; } = 30;

    /// <summary>
    /// Sunucunun kendi belleğindeki kopyanın süresi. Kısa tutulur: birden çok sunucuda geçersiz kılma diğer sunucuların
    /// belleğine en geç bu süre sonunda yansır.
    /// </summary>
    public int LocalMenuMinutes { get; init; } = 2;
}

/// <summary>
/// HybridCache ile iki seviyeli önbellek: birinci seviye sunucunun belleği, ikinci seviye Redis. Her kayıt
/// "menu:{id}" etiketini taşır; bir menünün bütün dilleri bu etiketle tek seferde geçersiz kılınır.
/// </summary>
internal sealed class HybridMenuCache(HybridCache cache, IOptions<CachingOptions> options) : IMenuCache
{
    private readonly HybridCacheEntryOptions _entryOptions = new()
    {
        Expiration = TimeSpan.FromMinutes(options.Value.MenuMinutes),
        LocalCacheExpiration = TimeSpan.FromMinutes(Math.Min(options.Value.LocalMenuMinutes, options.Value.MenuMinutes))
    };

    // Kayıt yoksa üretmeden boş döner: HybridCache'te "yalnızca oku".
    private static readonly HybridCacheEntryOptions ReadOnly = new()
    {
        Flags = HybridCacheEntryFlags.DisableUnderlyingData
            | HybridCacheEntryFlags.DisableLocalCacheWrite
            | HybridCacheEntryFlags.DisableDistributedCacheWrite
    };

    public ValueTask<TranslatedMenuResponse?> GetAsync(Guid menuId, string language, CancellationToken cancellationToken) =>
        cache.GetOrCreateAsync(
            Key(menuId, language),
            _ => ValueTask.FromResult<TranslatedMenuResponse?>(null),
            ReadOnly,
            cancellationToken: cancellationToken);

    public ValueTask SetAsync(TranslatedMenuResponse menu, CancellationToken cancellationToken) =>
        cache.SetAsync<TranslatedMenuResponse?>(
            Key(menu.MenuId, menu.Language), menu, _entryOptions, [Tag(menu.MenuId)], cancellationToken);

    public ValueTask InvalidateAsync(Guid menuId, CancellationToken cancellationToken) =>
        cache.RemoveByTagAsync(Tag(menuId), cancellationToken);

    private static string Key(Guid menuId, string language) => $"menu:{menuId:N}:{language}";

    private static string Tag(Guid menuId) => $"menu:{menuId:N}";
}
