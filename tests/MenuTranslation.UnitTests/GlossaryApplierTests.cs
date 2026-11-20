using MenuTranslation.Application.Abstractions;
using MenuTranslation.Infrastructure.Translation;

namespace MenuTranslation.UnitTests;

public class GlossaryApplierTests
{
    [Fact]
    public void Protect_returns_null_when_no_term_appears()
    {
        Assert.Null(GlossaryApplier.Protect("Türk kahvesi", [new GlossaryEntry("Madımak", null)]));
    }

    [Fact]
    public void Protect_wraps_the_term_and_keeps_the_rest_of_the_sentence()
    {
        string? xml = GlossaryApplier.Protect("Bulgurlu madımak", [new GlossaryEntry("Madımak", null)]);

        Assert.Equal("Bulgurlu <g>madımak</g>", xml);
    }

    [Theory]
    [InlineData("ARABAŞI çorbası")]
    [InlineData("Arabaşi çorbası")]
    [InlineData("arabaşı çorbası")]
    public void Protect_ignores_case_and_turkish_dotted_and_dotless_i(string text)
    {
        string? xml = GlossaryApplier.Protect(text, [new GlossaryEntry("Arabaşı", null)]);

        Assert.NotNull(xml);
        Assert.StartsWith("<g>", xml);
    }

    [Fact]
    public void Protect_does_not_match_inside_a_longer_word()
    {
        Assert.Null(GlossaryApplier.Protect("Madımaklı pide", [new GlossaryEntry("Madımak", null)]));
    }

    [Fact]
    public void Protect_puts_the_replacement_in_place_of_the_term()
    {
        string? xml = GlossaryApplier.Protect("Sıcak parmak çöreği", [new GlossaryEntry("Parmak çöreği", "Finger bun")]);

        Assert.Equal("Sıcak <g>Finger bun</g>", xml);
    }

    [Fact]
    public void Protect_prefers_the_longest_term_when_terms_overlap()
    {
        GlossaryEntry[] glossary = [new("çöreği", "pastry"), new("Parmak çöreği", "Finger bun")];

        Assert.Equal("<g>Finger bun</g>", GlossaryApplier.Protect("Parmak çöreği", glossary));
    }

    [Fact]
    public void Protect_escapes_xml_characters_without_encoding_turkish_letters()
    {
        string? xml = GlossaryApplier.Protect("Çay & madımak <yeni>", [new GlossaryEntry("Madımak", null)]);

        Assert.Equal("Çay &amp; <g>madımak</g> &lt;yeni&gt;", xml);
    }

    [Fact]
    public void Restore_removes_tags_and_decodes_entities()
    {
        Assert.Equal("Tea & madımak <new>", GlossaryApplier.Restore("Tea &amp; <g>madımak</g> &lt;new&gt;"));
    }
}
