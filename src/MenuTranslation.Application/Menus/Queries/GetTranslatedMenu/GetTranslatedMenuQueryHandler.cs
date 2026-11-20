using Mediator;

using MenuTranslation.Application.Abstractions;
using MenuTranslation.Application.Translations;
using MenuTranslation.Domain.Common;
using MenuTranslation.Domain.Menus;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MenuTranslation.Application.Menus.Queries.GetTranslatedMenu;

/// <summary>
/// Akış: önbellek → veritabanındaki çeviriler → yalnızca eksik ya da eskimiş metinler için çeviri servisi.
/// Yeni çeviriler veritabanına yazılır; böylece önbellek boşalsa ya da başka bir sunucu istese bile tekrar çevrilmez.
/// </summary>
internal sealed class GetTranslatedMenuQueryHandler(
    IAppDbContext db,
    IMenuCache cache,
    TextTranslator translator,
    TimeProvider timeProvider,
    ILogger<GetTranslatedMenuQueryHandler> logger)
    : IQueryHandler<GetTranslatedMenuQuery, Result<TranslatedMenuResponse>>
{
    public async ValueTask<Result<TranslatedMenuResponse>> Handle(GetTranslatedMenuQuery query, CancellationToken cancellationToken)
    {
        if (await cache.GetAsync(query.MenuId, query.Language, cancellationToken) is { } cached)
        {
            return cached;
        }

        var menu = await db.Menus
            .AsNoTracking()
            .Where(m => m.Id == query.MenuId)
            .Select(m => new { m.Id, m.Name, m.SourceLanguage, m.CityId })
            .FirstOrDefaultAsync(cancellationToken);
        if (menu is null)
        {
            return MenuErrors.MenuNotFound;
        }

        // Yalnızca istenen dildeki çeviri yüklenir; yeni çeviriler bu takip edilen listeye eklenir.
        List<MenuItem> items = await db.MenuItems
            .Where(i => i.MenuId == menu.Id)
            .Include(i => i.Translations.Where(t => t.Language == query.Language))
            .OrderBy(i => i.SortOrder)
            .ToListAsync(cancellationToken);

        MenuTranslationState state = MenuTranslationState.Original;
        if (query.Language != menu.SourceLanguage)
        {
            state = await TranslateStaleItemsAsync(items, menu.SourceLanguage, query.Language, menu.CityId, cancellationToken);
        }

        TranslatedMenuResponse response = new(
            menu.Id, menu.Name, menu.SourceLanguage, query.Language, state,
            items.Select(item => ToResponse(item, state)).ToList());

        // Kısmi menü önbelleğe yazılmaz: servis düzelince sonraki istek eksikleri çevirir.
        if (state != MenuTranslationState.Partial)
        {
            await cache.SetAsync(response, cancellationToken);
        }

        return response;
    }

    private async Task<MenuTranslationState> TranslateStaleItemsAsync(
        List<MenuItem> items, string sourceLanguage, string language, int? cityId, CancellationToken cancellationToken)
    {
        List<MenuItem> stale = items
            .Where(i => !TranslationFreshness.IsFresh(i.Translations.SingleOrDefault(), i.TextUpdatedAt))
            .ToList();
        if (stale.Count == 0)
        {
            return MenuTranslationState.Complete;
        }

        IEnumerable<string> texts = stale.Select(i => i.Name)
            .Concat(stale.Where(i => i.Description is not null).Select(i => i.Description!));
        IReadOnlyDictionary<string, string>? translations =
            await translator.TranslateAsync(texts, sourceLanguage, language, cityId, cancellationToken);
        if (translations is null)
        {
            return MenuTranslationState.Partial;
        }

        DateTimeOffset now = timeProvider.GetUtcNow();
        foreach (MenuItem item in stale)
        {
            MenuItemTranslation? translation = item.Translations.SingleOrDefault();
            if (translation is null)
            {
                translation = new MenuItemTranslation { MenuItemId = item.Id, Language = language };
                item.Translations.Add(translation);
            }

            translation.Name = translations[item.Name];
            translation.Description = item.Description is null ? null : translations[item.Description];
            translation.TranslatedBy = TranslationSource.Machine;
            translation.TranslatedAt = now;
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueConstraintViolationException exception)
        {
            // Aynı menüyü aynı anda isteyen başka bir istek çeviriyi önce yazdı. Bu istek elindeki çeviriyle yanıt
            // verir; veritabanındaki kayıt diğer isteğinkidir ve bir sonraki okumada o kullanılır.
            logger.LogInformation(exception, "Çeviri eşzamanlı bir istekte zaten yazılmış.");
        }

        return MenuTranslationState.Complete;
    }

    private static TranslatedMenuItem ToResponse(MenuItem item, MenuTranslationState state)
    {
        MenuItemTranslation? translation = item.Translations.SingleOrDefault();
        bool useTranslation = state != MenuTranslationState.Original
            && TranslationFreshness.IsFresh(translation, item.TextUpdatedAt);

        return new TranslatedMenuItem(
            item.Id,
            useTranslation ? translation!.Name : item.Name,
            useTranslation ? translation!.Description : item.Description,
            item.PriceCents,
            item.Name,
            useTranslation ? translation!.TranslatedBy : null,
            TranslationFreshness.IsOwnerTranslationOutdated(translation, item.TextUpdatedAt));
    }
}
