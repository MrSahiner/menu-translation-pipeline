namespace MenuTranslation.Domain.Menus;

/// <summary>Çeviriyi kimin yaptığı.</summary>
public enum TranslationSource
{
    /// <summary>Çeviri servisi; metin değişince yeniden çevrilir.</summary>
    Machine = 0,

    /// <summary>İşletmenin düzelttiği çeviri; otomatik çeviri bunu hiçbir zaman ezmez.</summary>
    Owner = 1
}

/// <summary>Bir ürünün bir dildeki çevirisi. Anahtar: (<see cref="MenuItemId"/>, <see cref="Language"/>).</summary>
public class MenuItemTranslation
{
    public Guid MenuItemId { get; set; }

    public string Language { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public TranslationSource TranslatedBy { get; set; }

    public DateTimeOffset TranslatedAt { get; set; }
}
