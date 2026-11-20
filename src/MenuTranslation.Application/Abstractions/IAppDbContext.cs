using MenuTranslation.Domain.Menus;
using MenuTranslation.Domain.Translations;

using Microsoft.EntityFrameworkCore;

namespace MenuTranslation.Application.Abstractions;

/// <summary>
/// Handler'ların veri erişimi. Ayrı bir repository katmanı yoktur; EF Core'un kendisi unit of work'tür.
/// Somut sınıf Infrastructure'dadır, Application yalnızca bu arayüzü bilir.
/// </summary>
public interface IAppDbContext
{
    DbSet<Menu> Menus { get; }

    DbSet<MenuItem> MenuItems { get; }

    DbSet<MenuItemTranslation> MenuItemTranslations { get; }

    DbSet<GlossaryTerm> GlossaryTerms { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Benzersizlik kısıtı ihlali. Infrastructure, veritabanına özgü hatayı buna çevirir; böylece Application
/// sağlayıcıya (Npgsql) bağımlı olmadan "aynı satırı başka istek yazdı" durumunu ele alabilir.
/// </summary>
public sealed class UniqueConstraintViolationException(string? constraintName, Exception innerException)
    : Exception($"Benzersizlik kısıtı ihlal edildi: {constraintName}", innerException)
{
    public string? ConstraintName { get; } = constraintName;
}
