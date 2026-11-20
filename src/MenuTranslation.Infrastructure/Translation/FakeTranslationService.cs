using System.Collections.Concurrent;
using System.Globalization;
using System.Text.RegularExpressions;

using MenuTranslation.Application.Abstractions;

namespace MenuTranslation.Infrastructure.Translation;

/// <summary>
/// Gerçek çeviri servisinin yerine geçen bilinçli olarak basit çevirici: Türkçeden İngilizceye küçük bir kelime
/// sözlüğüyle kelime kelime çevirir, sözlük terimlerini gerçek servisteki gibi XML etiketiyle korur. Gerçek üründe bu
/// arayüzü DeepL istemcisi uygular; hattın geri kalanı (önbellek, eskime, işletme çevirisi) aynı kalır.
/// Testler için servisin kapanmasını ve gecikmesini taklit edebilir, gönderilen metinleri kaydeder.
/// </summary>
public sealed class FakeTranslationService : ITranslationService
{
    private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");

    private static readonly Dictionary<string, string> TurkishToEnglish = new(StringComparer.Ordinal)
    {
        ["acılı"] = "spicy",
        ["baklava"] = "baklava",
        ["bulgurlu"] = "bulgur",
        ["çorbası"] = "soup",
        ["çörek"] = "pastry",
        ["fıstıklı"] = "pistachio",
        ["kahvesi"] = "coffee",
        ["közde"] = "ember",
        ["pişmiş"] = "brewed",
        ["tavuk"] = "chicken",
        ["tereyağlı"] = "buttery",
        ["türk"] = "turkish",
        ["usulü"] = "style",
        ["yozgat"] = "Yozgat"
    };

    private readonly ConcurrentQueue<string> _requestedTexts = new();
    private int _callCount;

    /// <summary>false ise her çağrı <see cref="TranslationUnavailableException"/> fırlatır.</summary>
    public bool IsAvailable { get; set; } = true;

    /// <summary>Her çağrıda yanıt öncesi beklenecek süre (süre sınırı testleri için).</summary>
    public TimeSpan Delay { get; set; } = TimeSpan.Zero;

    public int CallCount => _callCount;

    public IReadOnlyCollection<string> RequestedTexts => _requestedTexts.ToArray();

    public void Reset()
    {
        IsAvailable = true;
        Delay = TimeSpan.Zero;
        _requestedTexts.Clear();
        Interlocked.Exchange(ref _callCount, 0);
    }

    public async Task<IReadOnlyList<string>> TranslateAsync(
        IReadOnlyList<string> texts,
        string sourceLanguage,
        string targetLanguage,
        IReadOnlyList<GlossaryEntry> glossary,
        CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _callCount);
        foreach (string text in texts)
        {
            _requestedTexts.Enqueue(text);
        }

        if (Delay > TimeSpan.Zero)
        {
            await Task.Delay(Delay, cancellationToken);
        }

        if (!IsAvailable)
        {
            throw new TranslationUnavailableException("Sahte çeviri servisi kapalı.");
        }

        return texts.Select(text => Translate(text, sourceLanguage, targetLanguage, glossary)).ToList();
    }

    private static string Translate(string text, string sourceLanguage, string targetLanguage, IReadOnlyList<GlossaryEntry> glossary)
    {
        string xml = GlossaryApplier.Protect(text, glossary) ?? GlossaryApplier.Escape(text);
        if (sourceLanguage != "tr" || targetLanguage != "en")
        {
            return GlossaryApplier.Restore(xml);
        }

        // Etiketli (korunan) bölümler olduğu gibi kalır; aradaki kelimeler sözlükle çevrilir.
        string[] parts = Regex.Split(xml, $"(<{GlossaryApplier.Tag}>.*?</{GlossaryApplier.Tag}>)");
        string translated = string.Concat(parts.Select(part =>
            part.StartsWith($"<{GlossaryApplier.Tag}>", StringComparison.Ordinal)
                ? part
                : Regex.Replace(part, @"\p{L}+", match => TranslateWord(match.Value))));

        return GlossaryApplier.Restore(translated);
    }

    private static string TranslateWord(string word)
    {
        if (!TurkishToEnglish.TryGetValue(word.ToLower(Turkish), out string? english))
        {
            return word;
        }

        return char.IsUpper(word[0]) ? char.ToUpperInvariant(english[0]) + english[1..] : english;
    }
}
