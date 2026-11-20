namespace MenuTranslation.Domain.Translations;

/// <summary>
/// Çeviri sözlüğü terimi: makine çevirisinin bozduğu yöresel yemek adları gibi. <see cref="Replacement"/> boşsa terim
/// metinde olduğu gibi kalır ("Arabaşı"); doluysa yerine bu karşılık konur ("Parmak çöreği" → "Finger bun").
/// </summary>
public class GlossaryTerm
{
    public int Id { get; set; }

    /// <summary>Boşsa her şehirde geçerlidir; doluysa yalnızca o şehrin menülerinde ve genel terimden önce gelir.</summary>
    public int? CityId { get; set; }

    public string SourceLanguage { get; set; } = null!;

    /// <summary>Boşsa bütün hedef dillerde geçerlidir; yalnızca karşılığı olmayan (orijinal kalan) terimlerde kullanılır.</summary>
    public string? TargetLanguage { get; set; }

    public string Term { get; set; } = null!;

    public string? Replacement { get; set; }
}
