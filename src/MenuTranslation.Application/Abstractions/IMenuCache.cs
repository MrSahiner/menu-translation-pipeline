using MenuTranslation.Application.Menus.Queries.GetTranslatedMenu;

namespace MenuTranslation.Application.Abstractions;

/// <summary>
/// Çevrilmiş menülerin önbelleği. Bir menünün bütün dilleri tek seferde geçersiz kılınır: ürün değişince hangi
/// dillerin önbellekte olduğunu bilmek gerekmez.
/// </summary>
public interface IMenuCache
{
    ValueTask<TranslatedMenuResponse?> GetAsync(Guid menuId, string language, CancellationToken cancellationToken);

    ValueTask SetAsync(TranslatedMenuResponse menu, CancellationToken cancellationToken);

    ValueTask InvalidateAsync(Guid menuId, CancellationToken cancellationToken);
}
