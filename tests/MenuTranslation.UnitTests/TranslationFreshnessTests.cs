using MenuTranslation.Domain.Menus;

namespace MenuTranslation.UnitTests;

public class TranslationFreshnessTests
{
    private static readonly DateTimeOffset TextUpdatedAt = new(2026, 11, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Missing_translation_is_not_fresh()
    {
        Assert.False(TranslationFreshness.IsFresh(null, TextUpdatedAt));
    }

    [Fact]
    public void Machine_translation_made_after_the_last_text_change_is_fresh()
    {
        Assert.True(TranslationFreshness.IsFresh(Translation(TranslationSource.Machine, TextUpdatedAt.AddMinutes(1)), TextUpdatedAt));
    }

    [Fact]
    public void Machine_translation_older_than_the_text_is_stale()
    {
        Assert.False(TranslationFreshness.IsFresh(Translation(TranslationSource.Machine, TextUpdatedAt.AddMinutes(-1)), TextUpdatedAt));
    }

    [Fact]
    public void Owner_translation_stays_fresh_but_is_flagged_for_review_after_the_text_changes()
    {
        MenuItemTranslation owner = Translation(TranslationSource.Owner, TextUpdatedAt.AddMinutes(-1));

        Assert.True(TranslationFreshness.IsFresh(owner, TextUpdatedAt));
        Assert.True(TranslationFreshness.IsOwnerTranslationOutdated(owner, TextUpdatedAt));
    }

    [Fact]
    public void Price_change_does_not_touch_the_text_timestamp()
    {
        MenuItem item = new() { Name = "Türk kahvesi", Description = "Közde pişmiş", TextUpdatedAt = TextUpdatedAt };

        bool changed = item.UpdateText("Türk kahvesi", "Közde pişmiş", TextUpdatedAt.AddDays(1));

        Assert.False(changed);
        Assert.Equal(TextUpdatedAt, item.TextUpdatedAt);
    }

    [Fact]
    public void Text_change_moves_the_text_timestamp()
    {
        MenuItem item = new() { Name = "Türk kahvesi", TextUpdatedAt = TextUpdatedAt };
        DateTimeOffset now = TextUpdatedAt.AddDays(1);

        Assert.True(item.UpdateText("Közde Türk kahvesi", null, now));
        Assert.Equal(now, item.TextUpdatedAt);
    }

    private static MenuItemTranslation Translation(TranslationSource source, DateTimeOffset translatedAt) => new()
    {
        Language = "en",
        Name = "Turkish coffee",
        TranslatedBy = source,
        TranslatedAt = translatedAt
    };
}
