using MenuTranslation.Domain.Menus;
using MenuTranslation.Domain.Translations;

using Microsoft.EntityFrameworkCore;

namespace MenuTranslation.Infrastructure.Persistence;

/// <summary>Yerel çalıştırma için örnek veri: Yozgat'ta bir kafe ve yöresel yemek adlarını koruyan sözlük.</summary>
public static class DemoData
{
    public static readonly Guid MenuId = Guid.Parse("6f1c2a3e-0000-4000-8000-000000000001");

    /// <summary>Örnek şehir kimliği (Yozgat'ın plaka kodu).</summary>
    public const int YozgatCityId = 66;

    public static async Task SeedAsync(AppDbContext db, TimeProvider timeProvider, CancellationToken cancellationToken = default)
    {
        if (!await db.GlossaryTerms.AnyAsync(cancellationToken))
        {
            db.GlossaryTerms.AddRange(
                // Yalnızca Yozgat menülerinde: başka şehirde "arabaşı" farklı bir yemeği anlatabilir.
                new GlossaryTerm { CityId = YozgatCityId, SourceLanguage = "tr", Term = "Arabaşı" },
                new GlossaryTerm { SourceLanguage = "tr", Term = "Madımak" },
                new GlossaryTerm { SourceLanguage = "tr", TargetLanguage = "en", Term = "Parmak çöreği", Replacement = "Finger bun" });
        }

        if (!await db.Menus.AnyAsync(m => m.Id == MenuId, cancellationToken))
        {
            DateTimeOffset now = timeProvider.GetUtcNow();
            db.Menus.Add(new Menu
            {
                Id = MenuId,
                Name = "Saat Kulesi Kahvecisi",
                SourceLanguage = "tr",
                CityId = YozgatCityId,
                Items =
                [
                    Item(1, "Arabaşı çorbası", "Yozgat usulü acılı tavuk çorbası", 16000, now),
                    Item(2, "Madımak", "Bulgurlu madımak", 14000, now),
                    Item(3, "Parmak çöreği", "Tereyağlı çörek", 7000, now),
                    Item(4, "Türk kahvesi", "Közde pişmiş", 9000, now),
                    Item(5, "Fıstıklı baklava", null, 18000, now)
                ]
            });
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static MenuItem Item(int order, string name, string? description, int priceCents, DateTimeOffset now) => new()
    {
        Id = Guid.CreateVersion7(),
        Name = name,
        Description = description,
        PriceCents = priceCents,
        SortOrder = order,
        TextUpdatedAt = now
    };
}
