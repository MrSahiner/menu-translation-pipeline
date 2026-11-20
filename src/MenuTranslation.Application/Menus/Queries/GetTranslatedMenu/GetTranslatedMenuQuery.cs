using FluentValidation;

using Mediator;

using MenuTranslation.Domain.Common;

namespace MenuTranslation.Application.Menus.Queries.GetTranslatedMenu;

/// <summary>Menüyü istenen dilde döndürür; eksik ya da eskimiş çevirileri o anda çevirir.</summary>
public sealed record GetTranslatedMenuQuery(Guid MenuId, string Language) : IQuery<Result<TranslatedMenuResponse>>;

internal sealed class GetTranslatedMenuQueryValidator : AbstractValidator<GetTranslatedMenuQuery>
{
    public GetTranslatedMenuQueryValidator()
    {
        RuleFor(q => q.Language).Matches("^[a-z]{2}$").WithMessage("Dil iki küçük harfli kod olmalı, ör. \"en\".");
    }
}
