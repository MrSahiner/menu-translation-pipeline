using MenuTranslation.Application.Abstractions;
using MenuTranslation.Domain.Menus;
using MenuTranslation.Domain.Translations;

using Microsoft.EntityFrameworkCore;

using Npgsql;

namespace MenuTranslation.Infrastructure.Persistence;

/// <summary>PostgreSQL bağlamı. Tablo ve kısıt adları yapılandırma sınıflarında açıkça verilir.</summary>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IAppDbContext
{
    public DbSet<Menu> Menus => Set<Menu>();

    public DbSet<MenuItem> MenuItems => Set<MenuItem>();

    public DbSet<MenuItemTranslation> MenuItemTranslations => Set<MenuItemTranslation>();

    public DbSet<GlossaryTerm> GlossaryTerms => Set<GlossaryTerm>();

    /// <summary>PostgreSQL'in benzersizlik hatasını (23505) Application'ın tanıdığı istisnaya çevirir.</summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } postgres)
        {
            throw new UniqueConstraintViolationException(postgres.ConstraintName, exception);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
}
