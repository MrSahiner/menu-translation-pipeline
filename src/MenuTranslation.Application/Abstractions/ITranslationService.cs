namespace MenuTranslation.Application.Abstractions;

/// <summary>
/// Makine çevirisi. Gerçek üründe DeepL gibi bir servisle uygulanır; bu örnekte Infrastructure'daki sahte çevirici
/// kullanılır. Metinler tek istekte, gönderildikleri sırayla çevrilir.
/// </summary>
public interface ITranslationService
{
    /// <summary>
    /// <paramref name="texts"/>'i çevirir; sonuç aynı sırada ve aynı sayıdadır. Servis çeviri yapamıyorsa
    /// (anahtar yok, kota doldu, ağ hatası) <see cref="TranslationUnavailableException"/> fırlatır.
    /// </summary>
    /// <param name="glossary">Metinde geçerse çevrilmeyecek ya da sabit karşılıkla değiştirilecek terimler.</param>
    Task<IReadOnlyList<string>> TranslateAsync(
        IReadOnlyList<string> texts,
        string sourceLanguage,
        string targetLanguage,
        IReadOnlyList<GlossaryEntry> glossary,
        CancellationToken cancellationToken);
}

/// <summary>
/// Sözlük terimi: metinde kelime olarak geçerse (büyük/küçük harf ve ı/i farkı gözetmeden) çeviriye bırakılmaz.
/// <paramref name="Replacement"/> boşsa terim yazıldığı gibi kalır; doluysa yerine bu karşılık konur.
/// </summary>
public sealed record GlossaryEntry(string Term, string? Replacement);

/// <summary>Çeviri servisi şu an kullanılamıyor; çağıran taraf orijinal metinle devam eder.</summary>
public sealed class TranslationUnavailableException(string message, Exception? innerException = null)
    : Exception(message, innerException);
