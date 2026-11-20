using FluentValidation;

using Mediator;

using MenuTranslation.Application.Abstractions;
using MenuTranslation.Domain.Common;
using MenuTranslation.Domain.Menus;

using Microsoft.EntityFrameworkCore;

namespace MenuTranslation.Application.Menus.Commands.UpdateMenuItem;

/// <summary>
/// İşletme ürünü günceller. Yalnızca ad ya da açıklama değiştiyse o ürünün makine çevirileri eskir; fiyat değişikliği
/// çeviri gerektirmez. Her durumda menünün önbelleği boşaltılır.
/// </summary>
public sealed record UpdateMenuItemCommand(Guid MenuId, Guid ItemId, string Name, string? Description, int PriceCents)
    : ICommand<Result>;

internal sealed class UpdateMenuItemCommandValidator : AbstractValidator<UpdateMenuItemCommand>
{
    public UpdateMenuItemCommandValidator()
    {
        RuleFor(c => c.Name).NotEmpty().MaximumLength(120);
        RuleFor(c => c.Description).MaximumLength(300);
        RuleFor(c => c.PriceCents).GreaterThanOrEqualTo(0);
    }
}

internal sealed class UpdateMenuItemCommandHandler(IAppDbContext db, IMenuCache cache, TimeProvider timeProvider)
    : ICommandHandler<UpdateMenuItemCommand, Result>
{
    public async ValueTask<Result> Handle(UpdateMenuItemCommand command, CancellationToken cancellationToken)
    {
        MenuItem? item = await db.MenuItems
            .FirstOrDefaultAsync(i => i.Id == command.ItemId && i.MenuId == command.MenuId, cancellationToken);
        if (item is null)
        {
            return MenuErrors.ItemNotFound;
        }

        item.UpdateText(command.Name.Trim(), command.Description?.Trim(), timeProvider.GetUtcNow());
        item.PriceCents = command.PriceCents;
        await db.SaveChangesAsync(cancellationToken);

        await cache.InvalidateAsync(command.MenuId, cancellationToken);
        return Result.Success();
    }
}
