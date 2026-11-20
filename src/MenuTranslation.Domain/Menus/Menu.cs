namespace MenuTranslation.Domain.Menus;

/// <summary>Bir mekanın menüsü. İşletme menüyü kendi dilinde (<see cref="SourceLanguage"/>) girer.</summary>
public class Menu
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    /// <summary>Menünün yazıldığı dil, ör. "tr".</summary>
    public string SourceLanguage { get; set; } = null!;

    /// <summary>Mekanın şehri; şehre özel sözlük terimleri bununla seçilir.</summary>
    public int? CityId { get; set; }

    public List<MenuItem> Items { get; set; } = [];
}
