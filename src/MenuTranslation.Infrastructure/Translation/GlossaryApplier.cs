using System.Net;
using System.Text;
using System.Text.RegularExpressions;

using MenuTranslation.Application.Abstractions;

namespace MenuTranslation.Infrastructure.Translation;

/// <summary>
/// Sözlük terimlerini çeviri servisine "çevirme" diye işaretler. Metin XML'e çevrilir ve terim <c>&lt;g&gt;…&lt;/g&gt;</c>
/// içine alınır; DeepL gibi servisler bu etiketin içini olduğu gibi bırakır ("Madımak yemeği" → "Madımak dish").
/// Dönüşte etiketler kaldırılır. Böylece servis tarafında model eğitmeden terim koruması sağlanır.
/// </summary>
internal static class GlossaryApplier
{
    public const string Tag = "g";

    /// <summary>Terimleri işaretlenmiş XML metni; hiçbir terim geçmiyorsa null (metin olduğu gibi gönderilir).</summary>
    public static string? Protect(string text, IReadOnlyList<GlossaryEntry> glossary)
    {
        if (glossary.Count == 0)
        {
            return null;
        }

        // Uzun terimler önce: "Parmak çöreği" içindeki daha kısa bir terim ayrıca işaretlenmez.
        List<(int Start, int Length, string Value)> matches = [];
        foreach (GlossaryEntry entry in glossary.OrderByDescending(e => e.Term.Length))
        {
            foreach (Match match in Pattern(entry.Term).Matches(text))
            {
                bool overlaps = matches.Any(m => match.Index < m.Start + m.Length && m.Start < match.Index + match.Length);
                if (!overlaps)
                {
                    matches.Add((match.Index, match.Length, entry.Replacement ?? match.Value));
                }
            }
        }

        if (matches.Count == 0)
        {
            return null;
        }

        StringBuilder xml = new();
        int position = 0;
        foreach ((int start, int length, string value) in matches.OrderBy(m => m.Start))
        {
            xml.Append(Escape(text[position..start]));
            xml.Append('<').Append(Tag).Append('>').Append(Escape(value)).Append("</").Append(Tag).Append('>');
            position = start + length;
        }

        xml.Append(Escape(text[position..]));
        return xml.ToString();
    }

    /// <summary>
    /// Yalnızca XML için zorunlu üç karakter kodlanır. HtmlEncode Türkçe harfleri de sayısal koda çevirirdi
    /// ("ç" → "&amp;#231;"); hem gereksiz hem servisin bu kodları nasıl çevireceği belirsiz.
    /// </summary>
    public static string Escape(string text) =>
        text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

    /// <summary>Servisin XML yanıtından etiketleri kaldırır ve karakter kodlarını çözer.</summary>
    public static string Restore(string xml) =>
        WebUtility.HtmlDecode(xml.Replace($"<{Tag}>", string.Empty).Replace($"</{Tag}>", string.Empty));

    /// <summary>
    /// Terimi kelime olarak arar (önünde ve arkasında harf yok). Büyük/küçük harf gözetilmez; Türkçe ı/i/I/İ birbirinin
    /// yerine geçebilir: "ARABAŞI" ve "Arabaşi" de "Arabaşı" terimine uyar.
    /// </summary>
    private static Regex Pattern(string term)
    {
        StringBuilder pattern = new(@"(?<!\p{L})");
        foreach (char c in term)
        {
            pattern.Append(c is 'i' or 'ı' or 'I' or 'İ' ? "[iıIİ]" : Regex.Escape(c.ToString()));
        }

        pattern.Append(@"(?!\p{L})");
        return new Regex(pattern.ToString(), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }
}
