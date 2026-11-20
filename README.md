# Menu translation pipeline

A small, self-contained ASP.NET Core sample that shows how restaurant menus are served in the guest's language: translations are made on demand, stored, cached in two levels, kept up to date when the menu changes, and never overwrite a correction made by the business.

It is a simplified, rewritten slice of **[Coffee Go](https://mrsahiner.github.io/en/projects/coffee-go/)**, a multilingual QR menu platform I'm building. The product repository is private; this sample keeps the same architecture and rules with a fake translation service, so it runs without any API key.

## What it demonstrates

| Rule | Where |
| --- | --- |
| Only missing or outdated texts are sent to the translation service; a price change never triggers a translation | `GetTranslatedMenuQueryHandler`, `TranslationFreshness`, `MenuItem.UpdateText` |
| Local dish names survive translation through a city-aware glossary ("Arabaşı çorbası" → "Arabaşı soup") | `TextTranslator`, `GlossaryApplier` |
| A translation corrected by the business is never overwritten; if the original text changes later, it is flagged for review instead | `OverrideTranslationCommand`, `TranslationFreshness` |
| Two-level cache (in-memory + Redis via `HybridCache`); all languages of a menu are invalidated with one tag | `HybridMenuCache` |
| If the service is down or slower than the timeout, the menu is served in its original language and not cached, so the next request retries | `TextTranslator`, `GetTranslatedMenuQueryHandler` |
| Concurrent first requests that write the same translation do not fail | `AppDbContext.SaveChangesAsync`, `UniqueConstraintViolationException` |

## Architecture

Onion architecture with CQRS. Dependencies only point inwards.

```
src/
├── MenuTranslation.Domain          entities, freshness rule, Result/Error types (no dependencies)
├── MenuTranslation.Application     queries, commands, validators (Mediator + FluentValidation), abstractions
├── MenuTranslation.Infrastructure  EF Core (PostgreSQL), HybridCache + Redis, glossary, fake translator
└── MenuTranslation.Api             minimal API endpoints, ProblemDetails
tests/
├── MenuTranslation.UnitTests        glossary matching, freshness rule, fake translator
└── MenuTranslation.IntegrationTests real PostgreSQL and Redis via Testcontainers
```

```
GET /menus/{id}?lang=en
  └─ cache hit? ── yes ──▶ response
        │ no
        ▼
  load items + "en" translations
        ▼
  stale = no translation, or machine translation older than the text   (owner translations are always kept)
        ▼
  translate stale texts once, with glossary and timeout ── failed ──▶ original text, state = Partial, not cached
        ▼
  save translations ─▶ cache (all languages tagged "menu:{id}") ─▶ response
```

Expected failures (not found, validation, conflict) are returned as `Result` values and mapped to ProblemDetails; exceptions are reserved for the unexpected.

## Run it

Requirements: .NET 10 SDK and Docker.

```bash
docker compose up -d
dotnet run --project src/MenuTranslation.Api --launch-profile http
```

The database schema and a demo menu are created on startup. Then:

```bash
# the menu in its own language
curl "http://localhost:5080/menus/6f1c2a3e-0000-4000-8000-000000000001/?lang=tr"

# translated: "Arabaşı soup", "Madımak", "Finger bun", "Turkish coffee", ...
curl "http://localhost:5080/menus/6f1c2a3e-0000-4000-8000-000000000001/?lang=en"
```

| Method | Route | Purpose |
| --- | --- | --- |
| `GET` | `/menus/{menuId}?lang={code}` | Menu in the requested language |
| `PUT` | `/menus/{menuId}/items/{itemId}` | Business updates an item (`name`, `description`, `priceCents`) |
| `PUT` | `/menus/{menuId}/items/{itemId}/translations/{lang}` | Business corrects a translation (`name`, `description`) |

Authentication is left out to keep the sample focused; in the product, business endpoints are restricted to venue members.

## Tests

```bash
dotnet test
```

Integration tests start their own PostgreSQL and Redis containers with Testcontainers, so Docker must be running. The fake translator can be switched off, slowed down and inspected, which is how the fallback, timeout and "only stale texts are sent" rules are tested.

## Notes

- The fake translator is deliberately naive: it translates a handful of Turkish words into English, word by word. In the product, the same `ITranslationService` interface is implemented with DeepL, which honours the same XML tags used here to protect glossary terms.
- The schema is created with `EnsureCreated` to keep the sample short; the product uses EF Core migrations.

---

**Türkçe özet:** Bu repo, geliştirdiğim Coffee Go'nun çok dilli menü çeviri hattının sadeleştirilmiş, baştan yazılmış bir örneğidir. Çeviriler istek üzerine yapılır, veritabanına yazılır ve iki seviyeli önbellekte (bellek + Redis) tutulur. Yalnızca değişen metinler yeniden çevrilir; yöresel yemek adları şehre özel sözlükle korunur; işletmenin düzelttiği çeviri hiçbir zaman ezilmez. Çeviri servisi kapalıysa ya da geç kalırsa menü orijinal diliyle gösterilir. Gerçek PostgreSQL ve Redis üzerinde çalışan entegrasyon testleri içerir.
