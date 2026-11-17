# Course Academy

Online course academy for Bangladesh: course catalogue, checkout (bKash, Nagad, Rocket, card), learning area, course communities, certificates, and an admin panel with role- and screen-based access.

## Repository layout
| Folder | What it is |
|---|---|
| [`CourseAcademy.UI/`](CourseAcademy.UI) | Angular 22 frontend |
| [`CourseAcademy.API/`](CourseAcademy.API) | ASP.NET Core 10 Web API, EF Core, SQL Server ([README](CourseAcademy.API/README.md)) |
| [`docs/business/`](docs/business) | Business rules, data model and API contract — start with [README](docs/business/README.md) |
| [`docs/integration/`](docs/integration) | Streaming (media) service integration spec |

## Run locally
Requirements: Node.js and npm, .NET 10 SDK, SQL Server (local default instance, `Server=.`; database `CourseAcademyDB`).

```powershell
# 1. API (first time on a machine: set a JWT signing key, see CourseAcademy.API/README.md)
cd CourseAcademy.API
dotnet run --project CourseAcademy.Api --launch-profile http   # http://localhost:5162, Swagger at /swagger

# 2. Frontend (in a second terminal)
cd CourseAcademy.UI
npm install
npm start                                                      # http://localhost:4200
```
In development the API creates the database and a small demo set on start-up: `admin@courseacademy.demo` / `Admin@123`, `instructor@courseacademy.demo` / `Instructor@123`, `student@courseacademy.demo` / `Student@123`, and one course.

## Tests
```powershell
cd CourseAcademy.API; dotnet test
cd CourseAcademy.UI;  npm test
```

## Status
The backend is built step by step; features not on the API yet still use demo data in the browser. See [docs/business/CHANGELOG.md](docs/business/CHANGELOG.md).
