# AI_INSTRUCTIONS — libnode backend

## Контекст
Backend LibNode написан на `.NET 10`, использует `ASP.NET Core`, `EF Core`, `PostgreSQL`, JWT-аутентификацию и сервисную архитектуру без лишних промежуточных абстракций.

## Workspace Guardrails

- Read `/home/qustust/projects/libnodeProject/AGENTS.md` before this file.
- This repo is one implementation repository inside the LibNode workspace; do not commit parent `.planning/` from here.
- Before editing or committing on user request, run `git status --short`, `git diff`, and `git diff --staged` from `libnode/`.
- Use Docker-first acceptance through `../libnode-deployer/` for cross-service verification. Do not treat host-local `dotnet` commands as final cross-service proof.
- Do not read, print, or commit real `.env` files, local appsettings overrides, connection strings, JWT keys, API keys, logs, `TestResults/`, or coverage artifacts.
- Do not change backend runtime hardening, EF migrations, collection/progress invariants, reader navigation, or catalog pagination as part of Phase 1 workspace guidance.

## Базовая архитектура

### Слои и ответственность

- [CRITICAL] `Controllers/` принимают HTTP-запрос, читают route/query/body, навешивают `[Authorize]`, валидируют вход через DTO и возвращают HTTP-ответ. Контроллеры не содержат бизнес-логику, SQL, транзакции и прямую работу с `DbContext`.
- [CRITICAL] `Services/` — единственное место для бизнес-логики, инвариантов, доступа к данным, транзакций, идемпотентности и проверок владения ресурсами.
- [CRITICAL] `Data/AppDbContext.cs` — единственный источник истины для EF-конфигурации, индексов, ограничений, каскадов, UUIDv7 и audit-полей.
- [MANDATORY] `Models/Entities` — только ORM-сущности. `Models/DTOs` — только публичные контракты API. `Models/Enums` — перечисления предметной области. Возвращать Entity наружу запрещено.
- [FORBIDDEN] Вводить repository layer, generic repository, unit of work wrapper, mediator-обвязку или “helper service” поверх уже существующей схемы `Controller -> Service -> AppDbContext`, если это не продиктовано реальной архитектурной причиной.

## Пагинация

### Cursor Pagination — единственный нормальный стандарт

- [CRITICAL] Все новые списочные endpoint'ы обязаны использовать `CursorPagedResult<T, TCursor>` или `CursorStringPagedResult<T>`.
- [CRITICAL] Для каталога книг используется `CursorStringPagedResult<T>` с сортировкой по `CreatedAt` (по умолчанию), `UpdatedAt` или `Title`. Курсор — строка `sortValue|id`, где `id` — `Guid`-tie-breaker. Порядок по умолчанию — descending (новые сначала).
- [CRITICAL] `CursorPagedResult<T, TCursor>` остаётся для строго типизированных курсоров (например, `ChapterNumber` с `sortDesc`). `CursorStringPagedResult<T>` введён для сортировок по `DateTime`/`string`, потому что `CursorPagedResult<T, TCursor>` ограничен `struct`.
- [MANDATORY] Для курсорной выборки используй шаблон: фильтр по курсору -> сортировка -> `Take(limit + 1)` -> вычисление `HasMore` -> удаление лишнего элемента -> вычисление `NextCursor` по последнему реально возвращаемому элементу.
- [MANDATORY] Для глав курсор основан на `ChapterNumber` (`int`) с поддержкой `sortDesc`.
- [MANDATORY] `ChapterDetailDto` содержит `previousChapterId` и `nextChapterId`, вычисленные на backend по `ChapterNumber`; читалка не должна загружать весь список глав ради навигации.
- [FORBIDDEN] Вводить `Skip/Take`-based offset pagination в новые API “для простоты”.
- [FORBIDDEN] Сортировать cursor-ленты по произвольным полям без доказанного стабильного порядка.
- [MANDATORY] `PagedResult<T>` считать legacy/compatibility-моделью. Новые endpoint'ы на неё не проектировать.

## Ошибки и HTTP-контракт

### GlobalExceptionMiddleware и ProblemDetails

- [CRITICAL] Непредвиденные исключения должны доходить до `GlobalExceptionMiddleware`.
- [CRITICAL] Формат неожиданных server-side ошибок — `application/problem+json` (`ProblemDetails`, RFC 7807).
- [FORBIDDEN] Возвращать самодельные `500`-ответы из контроллеров или дублировать глобальную обработку ошибок локальными `try/catch`.
- [MANDATORY] Локально перехватывать только ожидаемые бизнес-ошибки и преобразовывать их в конкретные `4xx` (`401`, `403`, `404`, `409`, `400`).
- [MANDATORY] В development можно показывать детали исключения, но утечки внутренних деталей в production недопустимы.

## Безопасность

### Аутентификация, авторизация, секреты

- [CRITICAL] Защита endpoint'ов строится через `[Authorize]` и `[Authorize(Roles = "Admin")]`. Не изобретай альтернативные механизмы gatekeeping на уровне контроллеров.
- [MANDATORY] Для server-to-server интеграций допускается отдельная authentication scheme, но она должна регистрироваться через ASP.NET authentication pipeline и использовать `[Authorize(AuthenticationSchemes = "...")]`, а не ручную проверку ключей в контроллере.
- [CRITICAL] Пользовательский идентификатор всегда берётся из claims текущего токена. Никогда не принимай `userId` из body/query как источник истины.
- [MANDATORY] JWT-ключ и connection string приходят из конфигурации/переменных окружения. В репозитории допускаются только пустые заглушки.
- [MANDATORY] Секреты интеграций (`IntegrationAuth:*`, API keys внешних publisher/importer flows) приходят только из конфигурации/переменных окружения.
- [MANDATORY] Пароли хранятся только как `BCrypt` hash. Любое хранение или логирование plaintext-пароля запрещено.
- [MANDATORY] CORS настраивается только через конфиг origins. Безопасный fallback — localhost frontend. `AllowAnyOrigin()` в рабочем коде запрещён.

## Race Conditions, идемпотентность и транзакции

### Защита от гонок — обязательна

- [CRITICAL] Для сущностей с уникальными ограничениями нельзя делать шаблон “сначала `AnyAsync`, потом insert” как защиту от дублей. Это гонка.
- [CRITICAL] Защита от дублей строится на уникальных индексах БД и обработке `DbUpdateException` / `PostgresErrorCodes.UniqueViolation`.
- [MANDATORY] Идемпотентные операции должны быть реально идемпотентными. Если повторная запись допустима как no-op, дубликат нужно тихо схлопывать, а не превращать в `500`.
- [CRITICAL] Все multi-step операции, которые меняют несколько строк и обязаны быть атомарными, оборачиваются в явную транзакцию через `BeginTransactionAsync(...)` + `CommitAsync(...)`.
- [MANDATORY] Инвариант “одна книга находится только в одной коллекции пользователя” обеспечивается на backend. Frontend не является защитным слоем.
- [MANDATORY] Проверка владения коллекцией выполняется до мутаций; попытка работать с чужой коллекцией — это `UnauthorizedAccessException`/`Forbid`, а не “молчаливый успех”.

## Enum-поля и многие-ко-многим

### Модели метаданных книги

- [MANDATORY] Enum-поля сущностей (`BookType`, `OriginalStatus`, `TranslationStatus`) хранятся как `int` через `.HasConversion<int>()` в Fluent API. Enum-типы живут в `Models/Enums/`.
- [MANDATORY] Связь М:М между `Book` и `Tag`/`Category` настраивается через неявную join-таблицу EF Core (`HasMany().WithMany()`). Не создавай явную сущность-связку, если нет дополнительных полей на связи.
- [MANDATORY] `Tag` и `Category` имеют уникальный индекс на `Slug`. Защита от дублей — на уровне БД, а не через `AnyAsync`.
- [MANDATORY] При создании книги (`CreateBookDto`) теги и категории привязываются через `TagIds`/`CategoryIds` — сервис загружает их из БД и добавляет в коллекцию навигации.
- [MANDATORY] Если книга участвует во внешнем publishing/import flow, её стабильная внешняя идентичность хранится в `Book.Slug` с уникальным индексом; повторные интеграционные импорты должны опираться на этот slug, а не на title.

## EF Core lifecycle

### UUIDv7, audit-поля и Fluent API

- [CRITICAL] Генерация `Guid.CreateVersion7()` централизована в `AppDbContext.AddAuditInfo()`. Новые сущности с `Id` не должны получать ID вручную без крайней необходимости.
- [CRITICAL] `CreatedAt`, `UpdatedAt`, `AddedAt` выставляются централизованно в `SaveChanges/SaveChangesAsync`. Не дублируй это в сервисах, контроллерах или DTO-mapper'ах.
- [MANDATORY] `AddAuditInfo` не перезаписывает явно заданные `CreatedAt`/`AddedAt` при добавлении сущности и не перезаписывает `UpdatedAt`, если он был явно изменён вызывающей стороной. Это позволяет тестам и импортам задавать предметные даты безопасно, сохраняя физический `updatedAt` при обычных мутациях.
- [FORBIDDEN] Обходить `SaveChanges`-хуки сырыми bulk-операциями, если это ломает автоматическое выставление audit-полей/UUIDv7.
- [MANDATORY] Fluent API в `OnModelCreating` — источник истины для индексов, `IsRequired`, `MaxLength`, composite keys, внешних ключей и cascade behavior.
- [MANDATORY] SQL-defaults (`gen_random_uuid()`, `now()`) считаются safety net на стороне БД, но прикладной код всё равно обязан уважать централизованный lifecycle через `AppDbContext`.

## Работа с запросами и DTO

- [MANDATORY] Read-only запросы писать через `AsNoTracking()`.
- [MANDATORY] Проецируй в DTO прямо на уровне LINQ `Select`, а не загружай Entity в память ради ручного маппинга после этого.
- [MANDATORY] Все входные модели валидируются через `System.ComponentModel.DataAnnotations`.
- [MANDATORY] Контроллеры обязаны прокидывать `CancellationToken` вниз по стеку в сервисы и EF-запросы.
- [MANDATORY] Создающие endpoint'ы возвращают `CreatedAtAction(...)`, если это соответствует ресурсу.
- [MANDATORY] Внешние ingest endpoint'ы обязаны быть идемпотентными: повторный `create title` должен возвращать существующий ресурс по slug, а повторная загрузка главы — обновлять/схлопывать запись по `(BookId, ChapterNumber)`.
- [FORBIDDEN] Возвращать анонимные EF-сущности, навигации или “временные” поля, которых нет в DTO.

## Testing and Regression Harness

- [MANDATORY] Backend regression tests live in `LibNode.Api.Tests/` and must cover critical service/API invariants: EF migrations, collection idempotency/move, reading-progress upsert races, reader chapter neighbors, and catalog cursor pagination.
- [MANDATORY] Tests run host-local with `dotnet test LibNode.Api.Tests/LibNode.Api.Tests.csproj` or inside the disposable `api-tests` Docker service in `libnode-deployer`.
- [MANDATORY] Docker-first verification is the final acceptance surface for backend changes; host-local `dotnet` commands are iteration aids only.

## Production Hardening

### Middleware pipeline

- [MANDATORY] `Program.cs` must gate Swagger UI on `Swagger:Enabled` configuration (bound from `Swagger__Enabled` env var), not on `IsDevelopment()`.
- [MANDATORY] `ForwardedHeaders` middleware must be gated by `ForwardedHeaders:Enabled` configuration (bound from `ForwardedHeaders__Enabled` env var) and placed BEFORE `UseAuthentication` and `UseHttpsRedirection`.
- [MANDATORY] When `ForwardedHeaders` is enabled, clear `KnownIPNetworks` and `KnownProxies` for Docker internal network trust; external reverse proxies require explicit IP configuration.
- [MANDATORY] Rate limiting must be applied to specific sensitive controllers (`AuthController`, `ReaderIngestController`) using `[EnableRateLimiting("policy")]` with fixed-window policies, `QueueLimit = 0`, and `RejectionStatusCode = 429`.
- [MANDATORY] `UseRateLimiter()` must be placed AFTER `UseAuthentication()` and `UseAuthorization()` but BEFORE `MapControllers()`, and must itself be gated by `RateLimiting:Enabled`.
- [MANDATORY] `RequireHttpsMetadata` for JWT must be `!builder.Environment.IsDevelopment()` — `false` only in Development.
- [MANDATORY] `AllowedHosts` must be explicit in production (not `*`) and overridden via deployer env vars.

### Configuration

- [MANDATORY] `appsettings.json` must contain `Swagger`, `ForwardedHeaders`, and `RateLimiting` sections with safe defaults (`false`).
- [MANDATORY] `Cors:Origins` must be explicit; `AllowAnyOrigin()` is forbidden.

## Что нельзя ломать

- [FORBIDDEN] Убирать `GlobalExceptionMiddleware` из pipeline.
- [FORBIDDEN] Менять порядок auth middleware так, чтобы `UseAuthorization()` вызывался раньше `UseAuthentication()`.
- [FORBIDDEN] Переводить курсорные списки обратно на offset.
- [FORBIDDEN] Размазывать ownership checks, transaction logic и unique-violation handling по контроллерам.

## Обновление документации

- [MANDATORY] Добавил новый слой, новый кросс-срез, новый стандарт ошибок/авторизации/пагинации/транзакций — обнови этот файл немедленно.
