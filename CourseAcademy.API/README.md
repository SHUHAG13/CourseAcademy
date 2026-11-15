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
Requirements: .NET 10 SDK, SQL Server LocalDB (`(localdb)\MSSQLLocalDB`).

```powershell
dotnet run --project CourseAcademy.Api --launch-profile http
```
- API: http://localhost:5162/api/v1 — Swagger: http://localhost:5162/swagger — Health: http://localhost:5162/health
- In Development the database `CourseAcademy` is created and migrated automatically on start-up (`Database:MigrateOnStartup`).
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

## Authentication
- JWT access tokens (HS256, 15 minutes) carry only the user id. Permissions are read from the database on every request, so access changes apply immediately.
- Refresh tokens: random, stored as SHA-256 hashes, single use. 30 days with "Remember me", otherwise 24 hours. Reusing a used token ends all of that user's sessions.
- `register`, `login`, `refresh` are rate limited (`Auth:RequestsPerMinute`, default 20 per client address).
- Endpoint permissions are checked in services through `ICurrentUser.EnsureCanAsync`; `[RequirePermission]` is available for simple global checks.
- Development seeds the demo accounts (`Database:SeedDemoData`): `admin@courseacademy.demo` / `Admin@123`, `student@courseacademy.demo` / `Student@123`, `learner@courseacademy.demo` / `Learner@123`, `instructor@courseacademy.demo` and `sabbir@courseacademy.demo` / `Instructor@123`.

## Catalogue (Step 3)
- Public: `GET /api/v1/courses` (paged), `/courses/all`, `/courses/{slug}`, `/courses/{slug}/related`, `/instructors`. Locked lesson links, file-less items and meeting links are only sent to people with course access.
- Admin: `/api/v1/admin/courses` (create, update, publish, curriculum, delete), `/instructors` (create, update, delete).
- Development seeds the frontend's demo catalogue from `Data/Seed/demo-courses.json` (exported from the frontend's `courses.data.ts`), keeping course ids 1–4, and links the two demo instructor accounts to their profiles with course grants.
- Media columns: `ExternalUrl` (links) and `AssetId` (uploads, added with the media module).

### First run on a new machine
The JWT signing key is a secret and is not in the repository. Set one once (any random string of 32+ characters):
```powershell
dotnet user-secrets set "Jwt:SigningKey" "<random 64-character string>" --project CourseAcademy.Api
```

## Configuration (production)
Secrets never go in `appsettings.json`. Set them as environment variables or user secrets:

| Setting | Environment variable |
|---|---|
| Database connection | `ConnectionStrings__Default` |
| Allowed browser origins | `Cors__AllowedOrigins__0`, `Cors__AllowedOrigins__1`, … |
| JWT signing key (≥ 32 chars, random) | `Jwt__SigningKey` |

The API refuses to start without a connection string, at least one allowed origin, or a valid signing key. `Database:SeedDemoData` is refused outside Development.
