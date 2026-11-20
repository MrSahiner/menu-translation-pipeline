namespace MenuTranslation.Domain.Menus;

/// <summary>Bir çevirinin yeniden çevrilmeden kullanılıp kullanılamayacağına karar verir.</summary>
public static class TranslationFreshness
{
    /// <summary>
    /// Çeviri, işletmenin düzelttiği bir çeviriyse ya da metnin son değişikliğinden sonra yapıldıysa günceldir.
    /// Fiyat gibi metin dışı değişiklikler <see cref="MenuItem.TextUpdatedAt"/>'ı ilerletmediği için çeviriyi bozmaz.
    /// </summary>
    public static bool IsFresh(MenuItemTranslation? translation, DateTimeOffset textUpdatedAt) =>
        translation is not null
        && (translation.TranslatedBy == TranslationSource.Owner || translation.TranslatedAt >= textUpdatedAt);

    /// <summary>
    /// İşletmenin düzelttiği çeviri, orijinal metin sonradan değiştiği için gözden geçirilmeli mi?
    /// Çeviri yine gösterilir; panel bunu "kontrol gerekli" diye işaretler.
    /// </summary>
    public static bool IsOwnerTranslationOutdated(MenuItemTranslation? translation, DateTimeOffset textUpdatedAt) =>
        translation is { TranslatedBy: TranslationSource.Owner } && translation.TranslatedAt < textUpdatedAt;
}
