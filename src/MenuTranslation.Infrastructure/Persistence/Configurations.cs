using MenuTranslation.Domain.Menus;
using MenuTranslation.Domain.Translations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MenuTranslation.Infrastructure.Persistence;

internal sealed class MenuConfiguration : IEntityTypeConfiguration<Menu>
{
    public void Configure(EntityTypeBuilder<Menu> builder)
    {
        builder.ToTable("menus");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Name).HasMaxLength(120);
        builder.Property(m => m.SourceLanguage).HasMaxLength(2);
        builder.HasMany(m => m.Items).WithOne().HasForeignKey(i => i.MenuId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class MenuItemConfiguration : IEntityTypeConfiguration<MenuItem>
{
    public void Configure(EntityTypeBuilder<MenuItem> builder)
    {
        builder.ToTable("menu_items");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Name).HasMaxLength(120);
        builder.Property(i => i.Description).HasMaxLength(300);
        builder.HasMany(i => i.Translations).WithOne().HasForeignKey(t => t.MenuItemId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class MenuItemTranslationConfiguration : IEntityTypeConfiguration<MenuItemTranslation>
{
    public void Configure(EntityTypeBuilder<MenuItemTranslation> builder)
    {
        builder.ToTable("menu_item_translations");

        // Ürün başına dil başına tek çeviri; eşzamanlı iki istek aynı satırı yazarsa ikincisi bu kısıta takılır.
        builder.HasKey(t => new { t.MenuItemId, t.Language }).HasName("menu_item_translations_pk");
        builder.Property(t => t.Language).HasMaxLength(2);
        builder.Property(t => t.Name).HasMaxLength(200);
        builder.Property(t => t.Description).HasMaxLength(500);
        builder.Property(t => t.TranslatedBy).HasConversion<string>().HasMaxLength(16);
    }
}

internal sealed class GlossaryTermConfiguration : IEntityTypeConfiguration<GlossaryTerm>
{
    public void Configure(EntityTypeBuilder<GlossaryTerm> builder)
    {
        builder.ToTable("glossary_terms");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.SourceLanguage).HasMaxLength(2);
        builder.Property(t => t.TargetLanguage).HasMaxLength(2);
        builder.Property(t => t.Term).HasMaxLength(80);
        builder.Property(t => t.Replacement).HasMaxLength(80);
        builder.HasIndex(t => new { t.SourceLanguage, t.TargetLanguage, t.CityId });
    }
}
