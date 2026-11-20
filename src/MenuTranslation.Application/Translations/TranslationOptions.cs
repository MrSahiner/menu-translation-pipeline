namespace MenuTranslation.Application.Translations;

/// <summary>Çeviri hattı ayarları (<c>Translation</c> bölümü).</summary>
public sealed class TranslationOptions
{
    public const string SectionName = "Translation";

    /// <summary>Çeviri bu sürede bitmezse menü orijinal diliyle döner; misafir boş ekran beklemez.</summary>
    public int TimeoutMilliseconds { get; init; } = 5000;
}
