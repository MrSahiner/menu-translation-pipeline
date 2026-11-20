namespace MenuTranslation.Domain.Menus;

/// <summary>Menüdeki ürün. Çeviriler <see cref="Translations"/>'da dil başına bir kayıt olarak tutulur.</summary>
public class MenuItem
{
    public Guid Id { get; set; }

    public Guid MenuId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    /// <summary>Kuruş cinsinden fiyat. Fiyat çeviriyi etkilemez.</summary>
    public int PriceCents { get; set; }

    public int SortOrder { get; set; }

    /// <summary>Ad ya da açıklamanın son değiştiği an. Bundan eski makine çevirileri yeniden çevrilir.</summary>
    public DateTimeOffset TextUpdatedAt { get; set; }

    public List<MenuItemTranslation> Translations { get; set; } = [];

    /// <summary>
    /// Ad ve açıklamayı günceller. Metin gerçekten değiştiyse <see cref="TextUpdatedAt"/> ilerler ve true döner;
    /// aynı metin yeniden kaydedildiğinde mevcut çeviriler geçerli kalır.
    /// </summary>
    public bool UpdateText(string name, string? description, DateTimeOffset now)
    {
        if (name == Name && description == Description)
        {
            return false;
        }

        Name = name;
        Description = description;
        TextUpdatedAt = now;
        return true;
    }
}
