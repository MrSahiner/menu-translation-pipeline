using MenuTranslation.Application.Abstractions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MenuTranslation.Application.Translations;

/// <summary>
/// Bir grup metni tek seferde çevirir: aynı metinler bir kez gönderilir, sözlük uygulanır ve çağrıya süre sınırı
/// konur. Çeviri yapılamazsa null döner; çağıran orijinal metinle devam eder.
/// </summary>
internal sealed class TextTranslator(
    IAppDbContext db,
    ITranslationService translationService,
    IOptions<TranslationOptions> options,
    ILogger<TextTranslator> logger)
{
    /// <returns>Orijinal metin → çeviri; çeviri yapılamadıysa null.</returns>
    public async Task<IReadOnlyDictionary<string, string>?> TranslateAsync(
        IEnumerable<string> texts,
        string sourceLanguage,
        string targetLanguage,
        int? cityId,
        CancellationToken cancellationToken)
    {
        string[] distinct = texts.Distinct(StringComparer.Ordinal).ToArray();
        if (distinct.Length == 0)
        {
            return new Dictionary<string, string>();
        }

        IReadOnlyList<GlossaryEntry> glossary = await LoadGlossaryAsync(sourceLanguage, targetLanguage, cityId, cancellationToken);

        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(options.Value.TimeoutMilliseconds);

        IReadOnlyList<string> translated;
        try
        {
            translated = await translationService.TranslateAsync(distinct, sourceLanguage, targetLanguage, glossary, timeout.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // İsteği yapan vazgeçmedi, süre doldu: menü orijinal diliyle gösterilir, sonraki istekte yeniden denenir.
            logger.LogWarning(
                "Çeviri {Source}->{Target} {Timeout} ms içinde bitmedi; orijinal metin gösteriliyor.",
                sourceLanguage, targetLanguage, options.Value.TimeoutMilliseconds);
            return null;
        }
        catch (TranslationUnavailableException exception)
        {
            logger.LogWarning(exception, "Çeviri {Source}->{Target} şu an yapılamıyor; orijinal metin gösteriliyor.",
                sourceLanguage, targetLanguage);
            return null;
        }

        return distinct.Zip(translated).ToDictionary(pair => pair.First, pair => pair.Second, StringComparer.Ordinal);
    }

    /// <summary>
    /// Sözlük: bu kaynak dildeki genel terimler ve menünün şehrine özel terimler. Aynı terim birden çok kez varsa
    /// şehre özel olan, sonra hedef dile özel olan geçerlidir.
    /// </summary>
    private async Task<IReadOnlyList<GlossaryEntry>> LoadGlossaryAsync(
        string sourceLanguage, string targetLanguage, int? cityId, CancellationToken cancellationToken)
    {
        var terms = await db.GlossaryTerms
            .AsNoTracking()
            .Where(t => t.SourceLanguage == sourceLanguage
                && (t.TargetLanguage == null || t.TargetLanguage == targetLanguage)
                && (t.CityId == null || t.CityId == cityId))
            .Select(t => new { t.Term, t.Replacement, t.CityId, t.TargetLanguage })
            .ToListAsync(cancellationToken);

        return terms
            .OrderByDescending(t => t.CityId.HasValue)
            .ThenByDescending(t => t.TargetLanguage is not null)
            .DistinctBy(t => t.Term.ToLowerInvariant())
            .Select(t => new GlossaryEntry(t.Term, t.TargetLanguage is null ? null : t.Replacement))
            .ToList();
    }
}
