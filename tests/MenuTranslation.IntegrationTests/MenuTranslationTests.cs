using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

using MenuTranslation.Application.Abstractions;
using MenuTranslation.Application.Menus.Queries.GetTranslatedMenu;
using MenuTranslation.Domain.Menus;

using Microsoft.Extensions.DependencyInjection;

namespace MenuTranslation.IntegrationTests;

public sealed class MenuTranslationTests : IClassFixture<ApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public MenuTranslationTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        factory.Translator.Reset();
    }

    [Fact]
    public async Task First_request_translates_and_the_next_one_is_served_from_cache()
    {
        TestMenu menu = await _factory.CreateMenuAsync();

        TranslatedMenuResponse first = await GetMenuAsync(menu.Id, "en");
        TranslatedMenuResponse second = await GetMenuAsync(menu.Id, "en");

        Assert.Equal(MenuTranslationState.Complete, first.State);
        Assert.Equal(["Arabaşı soup", "Turkish coffee", "Pistachio baklava"], first.Items.Select(i => i.Name));
        Assert.Equal("Yozgat style spicy chicken soup", first.Items[0].Description);
        Assert.Equal(first.Items.Select(i => i.Name), second.Items.Select(i => i.Name));
        Assert.Equal(1, _factory.Translator.CallCount);
    }

    [Fact]
    public async Task Stored_translations_are_reused_when_the_cache_is_empty()
    {
        TestMenu menu = await _factory.CreateMenuAsync();
        await GetMenuAsync(menu.Id, "en");

        // Önbellek boşaldı (süre doldu ya da başka bir sunucu): çeviriler veritabanından gelir, servis çağrılmaz.
        await _factory.Services.GetRequiredService<IMenuCache>().InvalidateAsync(menu.Id, CancellationToken.None);
        TranslatedMenuResponse again = await GetMenuAsync(menu.Id, "en");

        Assert.Equal(MenuTranslationState.Complete, again.State);
        Assert.Equal(1, _factory.Translator.CallCount);
    }

    [Fact]
    public async Task Renaming_an_item_retranslates_only_that_item()
    {
        TestMenu menu = await _factory.CreateMenuAsync();
        await GetMenuAsync(menu.Id, "en");
        _factory.Translator.Reset();

        await PutAsync($"/menus/{menu.Id}/items/{menu.CoffeeId}", new { Name = "Közde Türk kahvesi", Description = "Közde pişmiş", PriceCents = 9000 });
        TranslatedMenuResponse updated = await GetMenuAsync(menu.Id, "en");

        // Önce eskimiş ürünlerin adları, sonra açıklamaları gönderilir; diğer iki ürün gönderilmez.
        Assert.Equal(["Közde Türk kahvesi", "Közde pişmiş"], _factory.Translator.RequestedTexts);
        Assert.Equal("Ember Turkish coffee", updated.Items[1].Name);
    }

    [Fact]
    public async Task Price_change_is_shown_without_calling_the_translation_service()
    {
        TestMenu menu = await _factory.CreateMenuAsync();
        await GetMenuAsync(menu.Id, "en");
        _factory.Translator.Reset();

        await PutAsync($"/menus/{menu.Id}/items/{menu.CoffeeId}", new { Name = "Türk kahvesi", Description = "Közde pişmiş", PriceCents = 9500 });
        TranslatedMenuResponse updated = await GetMenuAsync(menu.Id, "en");

        Assert.Equal(9500, updated.Items[1].PriceCents);
        Assert.Equal(0, _factory.Translator.CallCount);
    }

    [Fact]
    public async Task Owner_translation_is_never_overwritten_and_is_flagged_when_the_original_changes()
    {
        TestMenu menu = await _factory.CreateMenuAsync();
        await GetMenuAsync(menu.Id, "en");

        await PutAsync($"/menus/{menu.Id}/items/{menu.SoupId}/translations/en", new { Name = "Arabaşı (Yozgat chicken soup)", Description = "Spicy, served with dough cubes" });
        TranslatedMenuResponse corrected = await GetMenuAsync(menu.Id, "en");
        Assert.Equal("Arabaşı (Yozgat chicken soup)", corrected.Items[0].Name);
        Assert.Equal(TranslationSource.Owner, corrected.Items[0].TranslatedBy);
        Assert.False(corrected.Items[0].OwnerTranslationOutdated);

        _factory.Translator.Reset();
        await PutAsync($"/menus/{menu.Id}/items/{menu.SoupId}", new { Name = "Acılı Arabaşı çorbası", Description = "Yozgat usulü", PriceCents = 16000 });
        TranslatedMenuResponse afterRename = await GetMenuAsync(menu.Id, "en");

        Assert.Equal("Arabaşı (Yozgat chicken soup)", afterRename.Items[0].Name);
        Assert.True(afterRename.Items[0].OwnerTranslationOutdated);
        Assert.Equal(0, _factory.Translator.CallCount);
    }

    [Fact]
    public async Task Original_text_is_served_and_not_cached_while_the_service_is_down()
    {
        TestMenu menu = await _factory.CreateMenuAsync();

        _factory.Translator.IsAvailable = false;
        TranslatedMenuResponse fallback = await GetMenuAsync(menu.Id, "en");
        _factory.Translator.IsAvailable = true;
        TranslatedMenuResponse recovered = await GetMenuAsync(menu.Id, "en");

        Assert.Equal(MenuTranslationState.Partial, fallback.State);
        Assert.Equal("Arabaşı çorbası", fallback.Items[0].Name);
        Assert.Null(fallback.Items[0].TranslatedBy);
        Assert.Equal(MenuTranslationState.Complete, recovered.State);
        Assert.Equal("Arabaşı soup", recovered.Items[0].Name);
    }

    [Fact]
    public async Task Slow_service_does_not_keep_the_guest_waiting()
    {
        TestMenu menu = await _factory.CreateMenuAsync();
        _factory.Translator.Delay = TimeSpan.FromSeconds(5);

        Stopwatch stopwatch = Stopwatch.StartNew();
        TranslatedMenuResponse response = await GetMenuAsync(menu.Id, "en");
        stopwatch.Stop();

        Assert.Equal(MenuTranslationState.Partial, response.State);
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(3), $"Yanıt {stopwatch.Elapsed} sürdü.");
    }

    [Fact]
    public async Task Concurrent_first_requests_all_succeed()
    {
        TestMenu menu = await _factory.CreateMenuAsync();
        _factory.Translator.Delay = TimeSpan.FromMilliseconds(100);

        // Aynı anda gelen istekler aynı çeviri satırlarını yazmaya çalışır; benzersizlik kısıtı hatası yanıtı bozmamalı.
        TranslatedMenuResponse[] responses = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => GetMenuAsync(menu.Id, "en")));

        Assert.All(responses, r => Assert.Equal("Arabaşı soup", r.Items[0].Name));
    }

    [Fact]
    public async Task Source_language_is_returned_as_is()
    {
        TestMenu menu = await _factory.CreateMenuAsync();

        TranslatedMenuResponse response = await GetMenuAsync(menu.Id, "tr");

        Assert.Equal(MenuTranslationState.Original, response.State);
        Assert.Equal("Arabaşı çorbası", response.Items[0].Name);
        Assert.Equal(0, _factory.Translator.CallCount);
    }

    [Fact]
    public async Task Invalid_language_returns_a_validation_problem()
    {
        TestMenu menu = await _factory.CreateMenuAsync();

        HttpResponseMessage response = await _client.GetAsync($"/menus/{menu.Id}/?lang=english");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Unknown_menu_returns_not_found()
    {
        HttpResponseMessage response = await _client.GetAsync($"/menus/{Guid.NewGuid()}/?lang=en");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Owner_cannot_translate_into_the_menu_language()
    {
        TestMenu menu = await _factory.CreateMenuAsync();

        HttpResponseMessage response = await _client.PutAsJsonAsync(
            $"/menus/{menu.Id}/items/{menu.SoupId}/translations/tr", new { Name = "Çorba" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    private async Task<TranslatedMenuResponse> GetMenuAsync(Guid menuId, string language)
    {
        HttpResponseMessage response = await _client.GetAsync($"/menus/{menuId}/?lang={language}");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TranslatedMenuResponse>(Json))!;
    }

    private async Task PutAsync(string url, object body)
    {
        HttpResponseMessage response = await _client.PutAsJsonAsync(url, body);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }
}
