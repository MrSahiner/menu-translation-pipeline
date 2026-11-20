using MenuTranslation.Application.Abstractions;
using MenuTranslation.Infrastructure.Translation;

namespace MenuTranslation.UnitTests;

public class FakeTranslationServiceTests
{
    private readonly FakeTranslationService _service = new();

    [Fact]
    public async Task Translates_known_words_and_keeps_glossary_terms()
    {
        IReadOnlyList<string> result = await _service.TranslateAsync(
            ["Arabaşı çorbası", "Parmak çöreği", "Türk kahvesi"],
            "tr", "en",
            [new GlossaryEntry("Arabaşı", null), new GlossaryEntry("Parmak çöreği", "Finger bun")],
            CancellationToken.None);

        Assert.Equal(["Arabaşı soup", "Finger bun", "Turkish coffee"], result);
    }

    [Fact]
    public async Task Throws_unavailable_when_switched_off()
    {
        _service.IsAvailable = false;

        await Assert.ThrowsAsync<TranslationUnavailableException>(() =>
            _service.TranslateAsync(["Madımak"], "tr", "en", [], CancellationToken.None));
    }
}
