using FluentValidation;

using Mediator;

using MenuTranslation.Application.Abstractions;
using MenuTranslation.Domain.Common;
using MenuTranslation.Domain.Menus;

using Microsoft.EntityFrameworkCore;

namespace MenuTranslation.Application.Translations.Commands.OverrideTranslation;

/// <summary>
/// İşletme bir makine çevirisini düzeltir. Düzeltilen çeviri "işletme onaylı" olarak saklanır ve otomatik çeviri
/// onu ezmez; orijinal metin sonradan değişirse yalnızca gözden geçirilmesi gerektiği işaretlenir.
/// </summary>
public sealed record OverrideTranslationCommand(Guid MenuId, Guid ItemId, string Language, string Name, string? Description)
    : ICommand<Result>;

internal sealed class OverrideTranslationCommandValidator : AbstractValidator<OverrideTranslationCommand>
{
    public OverrideTranslationCommandValidator()
    {
        RuleFor(c => c.Language).Matches("^[a-z]{2}$").WithMessage("Dil iki küçük harfli kod olmalı, ör. \"en\".");
        RuleFor(c => c.Name).NotEmpty().MaximumLength(120);
        RuleFor(c => c.Description).MaximumLength(300);
    }
}

internal sealed class OverrideTranslationCommandHandler(IAppDbContext db, IMenuCache cache, TimeProvider timeProvider)
    : ICommandHandler<OverrideTranslationCommand, Result>
{
    public async ValueTask<Result> Handle(OverrideTranslationCommand command, CancellationToken cancellationToken)
    {
        var item = await db.MenuItems
            .Where(i => i.Id == command.ItemId && i.MenuId == command.MenuId)
            .Select(i => new { i.Id, SourceLanguage = db.Menus.Where(m => m.Id == i.MenuId).Select(m => m.SourceLanguage).First() })
            .FirstOrDefaultAsync(cancellationToken);
        if (item is null)
        {
            return MenuErrors.ItemNotFound;
        }

        if (command.Language == item.SourceLanguage)
        {
            return MenuErrors.SourceLanguage;
        }

        MenuItemTranslation? translation = await db.MenuItemTranslations
            .FirstOrDefaultAsync(t => t.MenuItemId == item.Id && t.Language == command.Language, cancellationToken);
        if (translation is null)
        {
            translation = new MenuItemTranslation { MenuItemId = item.Id, Language = command.Language };
            db.MenuItemTranslations.Add(translation);
        }

        translation.Name = command.Name.Trim();
        translation.Description = command.Description?.Trim();
        translation.TranslatedBy = TranslationSource.Owner;
        translation.TranslatedAt = timeProvider.GetUtcNow();
        await db.SaveChangesAsync(cancellationToken);

        await cache.InvalidateAsync(command.MenuId, cancellationToken);
        return Result.Success();
    }
}
