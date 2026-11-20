# Course Academy API

ASP.NET Core 10 Web API for Course Academy. The Angular frontend is in `../CourseAcademy.UI`. The repository's `docs/business/` folder is the source of truth for business rules, the data model (`data-model.md`) and the API contract (`api-contract.md`).

## Structure
One Web API project with a simple layered flow: **Controller → Service → Repository → AppDbContext**.

| Folder | Holds |
|---|---|
| `Controllers/` | Thin endpoints. Read the request, call a service, wrap the result (`ApiControllerBase`). |
| `Services/` | Business logic and permission checks. Throw `AppException` subclasses for expected failures. |
| `Repositories/` | Database access only. Generic `IRepository<T>`, specific repositories when a query needs it, `IUnitOfWork` to commit. |
| `Entities/` | Tables. Every entity inherits `BaseEntity` (`Id`, `CreatedAt`, `UpdatedAt`, `IsDeleted`). |
| `Dtos/` | Request/response models. Entities are never returned from controllers. |
| `Validators/` | FluentValidation validators, run automatically for every action argument. |
| `Data/` | `AppDbContext`, Fluent API configurations, seed data, migrations, the save interceptor. |
| `Common/` | Response envelope, exceptions, middleware, paging, options, DI and pipeline setup. |

## Conventions
- Every response: `{ "success", "data", "message", "errors": [{ "field", "message" }] }`. Lists: `data = { items, total, page, pageSize }`.
- Routes are versioned: `/api/v1/...`. Health check: `/health`.
- Times are UTC (`datetime2(3)`); money is `decimal(18,2)`.
- Deleting an entity soft-deletes it (`IsDeleted = 1`) unless it implements `IHardDelete`. Soft-deleted rows are hidden from all queries.
- Lookup values (formats, statuses, categories, payment methods) are tables seeded by migrations; the API uses their `Code`.
- Messages are English; the frontend translates them.
- Warnings are errors (`TreatWarningsAsErrors`).

## Run locally
Requirements: .NET 10 SDK, SQL Server (Development uses the local default instance: `ConnectionStrings:DefaultConnection` = `Server=.;Database=CourseAcademyDB;Trusted_Connection=True;…`).

```powershell
dotnet run --project CourseAcademy.Api --launch-profile http
```
- API: http://localhost:5162/api/v1 — API testing: Scalar http://localhost:5162/scalar (opens automatically with `dotnet run`) or Swagger http://localhost:5162/swagger (to test protected endpoints, log in with `POST /api/v1/auth/login`, then paste the `accessToken` under Authentication → Bearer in Scalar, or Authorize in Swagger) — Health: http://localhost:5162/health
- In Development the database `CourseAcademyDB` is created and migrated automatically on start-up (`Database:MigrateOnStartup`). Tables are grouped in schemas by area (`Securities`, `Lookups`, `Catalog`, `Commerce`, `Learning`, `Media`, `Certification`, `Community`), set in `Data/DbSchemas.cs`.
- Logs: console and `CourseAcademy.Api/logs/api-<date>.log`.

## Tests
```powershell
dotnet test
```

## Migrations
```powershell
dotnet tool install -g dotnet-ef --version 10.0.15   # once
dotnet ef migrations add <Name> --project CourseAcademy.Api --output-dir Data/Migrations
dotnet ef database update --project CourseAcademy.Api
```
Production: generate an idempotent script (`dotnet ef migrations script --idempotent`) and apply it as a release step; keep `Database:MigrateOnStartup` false.
The schema starts from one migration, `InitialCreate` (the step-by-step development migrations were merged into it before the first deployment). `AddCertificationAndCommunity` (Step 8) adds the certification and community tables on top, so an existing database keeps its data: run `dotnet ef database update` (Development also applies it on start-up).

## Security
- Every response carries `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY` and `Referrer-Policy: no-referrer`; outside Development HTTPS redirection and HSTS are on.
- Scalar, Swagger and the OpenAPI document (`/swagger/v1/swagger.json`), the demo payment gateway, the fake Streaming service and demo seeding are refused or off outside Development.
- Review of 2026-11-17: no known vulnerable packages (`dotnet list package --vulnerable`, `npm audit`); see `docs/security-review.md`.

## Authentication
- JWT access tokens (HS256, 15 minutes) carry only the user id. Permissions are read from the database on every request, so access changes apply immediately.
- Refresh tokens: random, stored as SHA-256 hashes, single use. 30 days with "Remember me", otherwise 24 hours. Reusing a used token ends all of that user's sessions.
- `register`, `login`, `refresh` are rate limited (`Auth:RequestsPerMinute`, default 20 per client address).
- Endpoint permissions are checked in services through `ICurrentUser.EnsureCanAsync`; `[RequirePermission]` is available for simple global checks.
- Development seeds one demo account per role (`Database:SeedDemoData`) when the API starts — `dotnet ef database update` alone adds only the lookup, screen and permission data: `admin@courseacademy.demo` / `Admin@123`, `instructor@courseacademy.demo` / `Instructor@123`, `student@courseacademy.demo` / `Student@123`.

## Catalogue (Step 3)
- Public: `GET /api/v1/courses` (paged), `/courses/all`, `/courses/{slug}`, `/courses/{slug}/related`, `/instructors`. Locked lesson links, file-less items and meeting links are only sent to people with course access.
- Admin: `/api/v1/admin/courses` (create, update, publish, curriculum, delete), `/instructors` (create, update, delete).
- Development seeds a small demo catalogue (`Data/Seed/DemoCourseSeeder.cs`): one instructor (linked to the instructor account, with a course grant), one published Hybrid course (1 module, 1 submodule, 2 sections, 2 videos, 1 document, 1 live class, 1 material) and one paid enrollment for the demo student. Lookup values, screens and permissions are seeded completely by the migration.
- Media columns: `ExternalUrl` (links) and `AssetId` (uploads, added with the media module).

## Payments (Step 4)
All payment rules (server-side price, one open payment per course, 30-minute expiry, access granted once, refunds) live in `Services/Payments/PaymentService.cs` and are the same for every provider. A provider is a plug-in implementing `IPaymentGateway`:

| Member | What it does |
|---|---|
| `Name` | Stored with each payment and used in the callback URL `POST /api/v1/payments/callback/{Name}` |
| `TransactionPrefix` | Start of the transaction numbers it creates, e.g. `SSL-20261115-K7Q2XM` |
| `AllowedInProduction` | `false` only for the demo gateway |
| `InitiateAsync` | Creates the provider's session and returns its hosted page URL |
| `VerifyCallbackAsync` | Checks the callback's signature (and, ideally, re-queries the provider) and returns the result, or `null` if not genuine |
| `QueryStatusAsync` | Asks the provider for a payment's status (used when the receipt is opened before the callback arrived) |

### Adding a real gateway (for example SSLCOMMERZ)
1. Create `Services/Payments/SslCommerzGateway.cs` implementing `IPaymentGateway`. Call the provider with a typed `HttpClient`.
2. Put its settings (store id, secret, sandbox URL) in a new options section, with the secrets in user secrets or environment variables.
3. Register it in `Common/ServiceCollectionExtensions.cs`: `services.AddSingleton<IPaymentGateway, SslCommerzGateway>();`
4. Set `Payments:Gateway` to its `Name` (for example `sslcommerz`) and give the provider the callback URL `https://<api-host>/api/v1/payments/callback/sslcommerz`.

No change to `PaymentService`, the controllers, the database or the Angular app is needed: the frontend already follows an external `redirectUrl`. `GatewayPluginTests` shows a made-up provider plugged in this way.

The demo gateway (`Payments:Gateway = demo`) is for Development only; the API refuses to start with it in any other environment. A background job cancels pending payments after `Payments:PendingMinutes`.

### First run on a new machine
The JWT signing key is a secret and is not in the repository. Set one once (any random string of 32+ characters):
```powershell
dotnet user-secrets set "Jwt:SigningKey" "<random 64-character string>" --project CourseAcademy.Api
```

## Progress (Step 5)
- Progress is stored per lesson in `LessonProgress`: each video is a lesson, and so is a section without videos. Course % = completed lessons ÷ all lessons still in the course (`LessonRules` in `Services/ProgressService.cs`).
- `GET /api/v1/progress/mine` (dashboard), `GET /progress/courses/{slug}`, `PUT /progress/videos/{id}/complete`, `PUT /progress/sections/{id}/complete`, `PUT /progress/videos/{id}/position`.
- Only video files (direct .mp4/.webm links, later uploads) send positions; the first time 90% is watched the video completes itself. YouTube/Vimeo videos are completed with the button.
- `GET /api/v1/admin/overview` (`reports.view`) returns the admin overview totals and the 5 newest payments.

## Media (Step 6)
Course files (videos, documents, materials) are kept by a separate **Streaming service** (NestJS), as described in `docs/integration/streaming-integration.md`. This API decides who may upload, watch or download, stores only Streaming's asset id and status (`MediaAsset`), and never stores or proxies file bytes.
- Endpoints: `/api/v1/media/uploads`, `/uploads/{id}/complete`, `/assets/{id}` (status, delete), `/videos/{id}/playback`, `/documents/{id}/download`, `/materials/{id}/download`; webhooks at `/api/v1/internal/streaming/webhooks` (HMAC-SHA256 signed, de-duplicated by event id, applied in sequence order).
- `Streaming:Mode = Fake` (Development default): a built-in stand-in stores files in `CourseAcademy.Api/App_Data/fake-streaming`, "processes" them in a few seconds and sends real signed webhooks to itself. Videos play as plain files. Refused outside Development.
- `Streaming:Mode = Http`: the real service at `Streaming:BaseUrl`, called with an `X-Api-Key` header and a resilience handler (retries for safe calls, timeouts, circuit breaker).
- Files no lesson or material uses any more are deleted from Streaming by a background worker (`MediaDeletionService`), at once and retried every minute.

## Certification and community (Step 8)
- Certification (`Certification` schema): `/api/v1/admin/courses/{id}/certification-rule` (get, save), `/me/courses/{id}/certification` (status), `/me/courses/{id}/certificate` (claim), `/me/courses/{id}/exam-submissions` (hand in), `/me/certificates`, `/admin/exam-submissions` (+ `/{id}/review`), `/admin/certificates` (+ `/{id}/revoke`, `/{id}/restore`), public `/certificates/verify/{code}`. Rules live in `Services/CertificationService.cs`.
- Completion certificates use the lesson % from `LessonRules` and are issued by the server when a lesson completes (`ProgressService` calls `ICertificationService.IssueForProgressAsync`). A revoked certificate blocks new ones until it is restored. Codes `CA-<UTC year>-XXXXXX`; one valid certificate per user and course (filtered unique index).
- Community (`Community` schema): `/courses/{slug}/community` (public), `/courses/{slug}/posts`, `/posts/{id}` (+ `/flags`, `/comments`), `/comments/{id}` (+ `/hidden`), `/admin/community/posts`, `/admin/community/comments`. Rules live in `Services/CommunityService.cs`; deleting a course soft-deletes its posts and comments.
- Development seed (`Data/Seed/DemoCertificationCommunitySeeder.cs`, added when the area is empty): an exam rule on the demo course, two more students who bought it and finished every lesson — `sample@courseacademy.demo` (passed, certificate `CA-<year>-K7Q2XM`) and `nusrat@courseacademy.demo` (hand-in waiting for review), both `Student@123` — and four posts with comments.

## Configuration (production)
Secrets never go in `appsettings.json`. Set them as environment variables or user secrets:

| Setting | Environment variable |
|---|---|
| Database connection | `ConnectionStrings__DefaultConnection` |
| Allowed browser origins | `Cors__AllowedOrigins__0`, `Cors__AllowedOrigins__1`, … |
| JWT signing key (≥ 32 chars, random) | `Jwt__SigningKey` |
| Streaming mode and address | `Streaming__Mode` = `Http`, `Streaming__BaseUrl` |
| Streaming API key (≥ 16 chars) | `Streaming__ApiKey` |
| Streaming webhook secret (≥ 32 chars, shared with Streaming) | `Streaming__WebhookSecret` |

The API refuses to start without a connection string, at least one allowed origin, or a valid signing key. `Database:SeedDemoData` is refused outside Development.
