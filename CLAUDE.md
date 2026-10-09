# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

Учебный проект курса «Соларлаб Академия Backend 2023»: REST API доски объявлений на ASP.NET Core (.NET 10), EF Core 10 + PostgreSQL. Документация, комментарии, сообщения об ошибках и коммиты — на русском.

## Команды

Все команды — из корня репозитория (там `Solarlab.Academy-2023.sln`).

```bash
dotnet build
dotnet test                                    # unit (Board.Tests) + интеграционные (Board.Api.Tests)
dotnet test --filter "FullyQualifiedName~Board.Api.Tests.CrudTests.Category_Crud_Succeeds"   # один тест
dotnet test src/Board/Tests/Board.Api.Tests    # один проект
```

Тестам не нужны ни PostgreSQL, ни секреты: интеграционные тесты поднимают API через `WebApplicationFactory` с InMemory-БД и сами задают `Jwt:Key`.

Локальный запуск API (окружение Development, строка подключения к localhost — в `appsettings.Development.json`):

```bash
cd src/Board/Host/Board.Host.Api
dotnet user-secrets set "Jwt:Key" "<не короче 32 символов>"   # без ключа API не стартует
dotnet run
```

Миграции (EF-инструмент `dotnet-ef` 10.x; запускать из проекта мигратора — он и есть startup/migrations-проект):

```bash
cd src/Board/Host/Board.Host.DbMigrator
dotnet ef migrations add <Name>
dotnet ef migrations has-pending-model-changes   # должно быть «No changes»
dotnet run                                       # применить миграции и выйти
```

Docker: `cp .env.example .env`, заполнить `POSTGRES_PASSWORD`, `JWT_KEY` (и при необходимости `ADMIN_LOGIN`, `AUTOMAPPER_LICENSE_KEY`), затем `docker compose up -d` (нужен Compose v2: используется `depends_on.condition`). API — на `localhost:5000`. `Test.js` — нагрузочный сценарий k6.

## Архитектура

Слоистая структура в `src/Board/`, зависимости направлены внутрь:

- **Domain** — EF-сущности `Account`, `Advert`, `Category` (дерево через `ParentId`), `File` (содержимое хранится в БД как `byte[]`).
- **Contracts** — DTO, атрибуты валидации (`ForbiddenWordsValidationAttribute` берёт `IForbiddenWordsService` из `ValidationContext`), `ErrorDto`, `PageRequestDto`.
- **Application/Board.Application.AppData** — сервисы и интерфейсы репозиториев по контекстам (`Contexts/{Account,Adverts,Categories,Files}`), прикладные исключения (`Common/Exceptions`), `ICurrentUserAccessor`, роли. Не зависит от ASP.NET и EF.
- **Infrastructure/Board.Infrastructure** — обобщённый `Repository<T>` поверх `DbContext` и профили AutoMapper.
- **Infrastructure/Board.Infrastructure.DataAccess** — `BoardDbContext` (конфигурации сущностей подхватываются из сборки через `ApplyConfigurationsFromAssembly`) и реализации репозиториев. Репозитории для чтения проецируют сразу в DTO через `ProjectTo`, для изменения отдают сущности.
- **Host/Board.Host.Api** — контроллеры, `Program.cs` (вся регистрация DI и pipeline), middleware, `HttpContextCurrentUserAccessor`.
- **Host/Board.Host.DbMigrator** — содержит все миграции (`MigrationDbContext : BoardDbContext`). Применяет миграции, затем хеширует пароли, оставшиеся в открытом виде (`LegacyPasswordHasher`), и завершается. API миграции не применяет.

### Сквозные механизмы, которые нужно знать

- **Ошибки.** Сервисы бросают исключения из `Common/Exceptions`; `ExceptionHandlingMiddleware` превращает их в `ErrorDto`: `EntityNotFoundException`→404, `BusinessRuleException`→422, `AccessDeniedException`→403, `InvalidCredentialsException`→401, прочее→500 без деталей. Ошибки валидации модели (400) формирует `InvalidModelStateResponseFactory` в `Program.cs`; 401/403 от JWT-аутентификации — `JwtBearerEvents`, тоже в формате `ErrorDto`. Контроллеры не возвращают `NotFound()` и т.п. — они полагаются на исключения.
- **Авторизация.** JWT (HS256, проверяются issuer/audience). Контроллеры помечены `[Authorize]`, чтение — `[AllowAnonymous]`. Объявления и файлы имеют автора (`AccountId`, может быть `null` у старых записей); изменять их может автор или администратор — проверка `ICurrentUserAccessor.CanModify` в сервисах. Категориями управляет только роль `Admin` (`[Authorize(Roles = Roles.Admin)]`). Администраторы — список логинов в `Administration:AdminLogins`, роль кладётся в токен при входе.
- **Аккаунты.** Пароли — PBKDF2 (`Pbkdf2PasswordHasher`, формат `{итерации}.{соль}.{хеш}`). Логин уникален без учёта регистра: поиск и уникальный индекс — по `NormalizedLogin` (`Account.NormalizeLogin`). Гонка при регистрации ловится по нарушению уникальности Postgres в `AccountRepository`.
- **PATCH.** JSON Patch на System.Text.Json (`Microsoft.AspNetCore.JsonPatch.SystemTextJson`). Схема в контроллере: `GetForUpdate` (с проверкой прав) → `ApplyTo` → `TryValidateModel` → `Update`.
- **Конфигурация, читаемая лениво.** Настройки JWT, лимиты загрузки файлов (`FileUpload`), rate limiting входа/регистрации (`RateLimiting:Auth`) читаются через options/`IConfiguration` при первом использовании, а не при построении `builder` — иначе тесты не могут их переопределить через `UseSetting`. Сохраняйте этот подход при добавлении новых настроек.
- **AutoMapper 16** — коммерческая лицензия, ключ в `AutoMapper:LicenseKey`; без ключа пишет предупреждение в лог. `AssertConfigurationIsValid` выполняется при старте API, так что новый маппинг с непокрытыми полями уронит запуск и интеграционные тесты.

### Тесты

- `Board.Api.Tests`: у каждой `BoardWebApplicationFactory` своя InMemory-БД (тестовые классы идут параллельно), сид с фиксированными идентификаторами в `DataSeedHelper`. Хелперы: `CreateAuthorizedClientAsync()` регистрирует нового пользователя и возвращает клиент с JWT, `CreateAdminClientAsync()` — клиент администратора. Лимит запросов к `Account/*` в тестах поднят; для проверки самого лимита используйте `WithWebHostBuilder(b => b.UseSetting(...))`.
- InMemory-провайдер не проверяет уникальные индексы, NOT NULL и внешние ключи — такое поведение проверяйте на настоящем PostgreSQL.
- `Board.Tests` — unit-тесты сервисов и атрибутов (xUnit, Moq, Shouldly).

### Миграции

EF 10 отказывается применять миграции, если модель разошлась со снимком, поэтому после любого изменения сущностей или их конфигураций (включая nullability свойств — в проектах включён `<Nullable>enable</Nullable>`, и EF выводит из неё `IsRequired`) нужна новая миграция. Сгенерированную миграцию стоит просмотреть: для существующих данных может понадобиться `RenameColumn` вместо drop/add или SQL-заполнение новых колонок (примеры — `AccountPasswordHashAndOwnership`, `CaseInsensitiveLogin`).

## CI/CD

`.github/workflows/build-test-deploy.yml`: build + test на .NET 10 при push и PR в `master`; деплой на VPS по SSH (`git pull` + `docker compose up -d --build`) только при push в `master`. На сервере должен лежать `.env`, иначе скрипт деплоя прерывается.
