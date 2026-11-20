using MenuTranslation.Domain.Menus;

namespace MenuTranslation.Application.Menus.Queries.GetTranslatedMenu;

/// <summary>Menünün hangi ölçüde çevrildiği.</summary>
public enum MenuTranslationState
{
    /// <summary>İstenen dil menünün kendi dili; çeviri gerekmedi.</summary>
    Original,

    /// <summary>Bütün ürünler istenen dilde.</summary>
    Complete,

    /// <summary>Çeviri servisine ulaşılamadı ya da süre doldu; bazı ürünler orijinal dilinde. Önbelleğe yazılmaz.</summary>
    Partial
}

public sealed record TranslatedMenuResponse(
    Guid MenuId,
    string Name,
    string SourceLanguage,
    string Language,
    MenuTranslationState State,
    IReadOnlyList<TranslatedMenuItem> Items);

/// <param name="TranslatedBy">Gösterilen metnin kaynağı; orijinal metin gösteriliyorsa null.</param>
/// <param name="OwnerTranslationOutdated">İşletme çevirisi, orijinal metin sonradan değiştiği için gözden geçirilmeli.</param>
public sealed record TranslatedMenuItem(
    Guid Id,
    string Name,
    string? Description,
    int PriceCents,
    string OriginalName,
    TranslationSource? TranslatedBy,
    bool OwnerTranslationOutdated);
