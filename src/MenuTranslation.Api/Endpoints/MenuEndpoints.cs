using Mediator;

using MenuTranslation.Application.Menus.Commands.UpdateMenuItem;
using MenuTranslation.Application.Menus.Queries.GetTranslatedMenu;
using MenuTranslation.Application.Translations.Commands.OverrideTranslation;

namespace MenuTranslation.Api.Endpoints;

/// <summary>
/// Uç noktalar yalnızca isteği komuta ya da sorguya çevirir; iş kuralı Application'dadır.
/// Örnek sade kalsın diye kimlik doğrulama yok; gerçek üründe işletme uç noktaları mekan üyeliğine bağlıdır.
/// </summary>
public static class MenuEndpoints
{
    public static IEndpointRouteBuilder MapMenuEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder menus = app.MapGroup("/menus/{menuId:guid}");

        // Misafir: menüyü kendi dilinde okur (QR menü).
        menus.MapGet("/", async (Guid menuId, string lang, ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetTranslatedMenuQuery(menuId, lang), ct)).ToHttpResult());

        // İşletme: ürünü günceller.
        menus.MapPut("/items/{itemId:guid}", async (Guid menuId, Guid itemId, UpdateMenuItemRequest body, ISender sender, CancellationToken ct) =>
            (await sender.Send(new UpdateMenuItemCommand(menuId, itemId, body.Name, body.Description, body.PriceCents), ct)).ToHttpResult());

        // İşletme: bir makine çevirisini düzeltir.
        menus.MapPut("/items/{itemId:guid}/translations/{language}", async (Guid menuId, Guid itemId, string language, OverrideTranslationRequest body, ISender sender, CancellationToken ct) =>
            (await sender.Send(new OverrideTranslationCommand(menuId, itemId, language, body.Name, body.Description), ct)).ToHttpResult());

        return app;
    }
}

public sealed record UpdateMenuItemRequest(string Name, string? Description, int PriceCents);

public sealed record OverrideTranslationRequest(string Name, string? Description);
