using MenuTranslation.Domain.Common;

namespace MenuTranslation.Domain.Menus;

/// <summary>Menü ve çeviri işlemlerinin beklenen hataları.</summary>
public static class MenuErrors
{
    public static readonly Error MenuNotFound = new("Menu.NotFound", "Menü bulunamadı.", ErrorType.NotFound);

    public static readonly Error ItemNotFound = new("MenuItem.NotFound", "Ürün bu menüde bulunamadı.", ErrorType.NotFound);

    public static readonly Error SourceLanguage = new(
        "Translation.SourceLanguage",
        "Menünün kendi dili çevrilemez; metni ürünün kendisinden düzenleyin.",
        ErrorType.Conflict);
}
