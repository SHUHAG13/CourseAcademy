# Course Academy

Online course academy (Bangladesh): course catalogue, checkout (bKash/Nagad/Rocket/card), learning area, course communities, certificates, and an admin panel with role/permission management. Angular frontend with browser-storage demo data; the ASP.NET Core 10 backend is being built step by step in the sibling folder `../CourseAcademy.Backend` (see its README).

## Stack
- Angular 22 (standalone components, signals, lazy routes), TypeScript 6
- Bootstrap 5 + Bootstrap Icons, SCSS
- jsPDF (certificate PDFs)
- Vitest + jsdom (unit tests via `ng test`)
- i18n: runtime Bangla/English (`src/app/core/i18n`)

## Folder structure
- `src/app/app.routes.ts` — all routes and guards
- `src/app/core/models` — domain interfaces/enums
- `src/app/core/services` — business logic + permission checks
- `src/app/core/payments` — payment service, gateway abstraction, demo gateway
- `src/app/core/data` — repositories (localStorage), seed/demo data, screens config
- `src/app/core/api` — `ApiService` (HttpClient wrapper: unwraps `{success,data,message,errors}`, throws `ApiError`), API models
- `src/environments` — `apiBaseUrl` and `useApi`. Development has `useApi: true` (needs the API running); production build still `false`. In API mode sign-in, users, roles and access use the API; other features still use browser-storage demo data, except certificates and community, which show "Coming soon". Services read the mode from the `API_MODE` token.
- `src/app/core/api/interceptors.ts` — `authInterceptor` (adds token, single shared refresh on 401) and `apiErrorInterceptor` (turns failures into `ApiError`)
- `src/app/core/guards`, `core/i18n`
- `src/app/pages` — routed pages (incl. `pages/admin`)
- `src/app/components` — home/marketing sections
- `src/app/shared` — reusable components, pipes, form validators
- `docs/business` — business documentation (see below)

## Commands
- `npm start` — dev server (`ng serve`)
- `npm run build` — production build
- `npm test` — unit tests (Vitest). Uses the `test` build configuration (production `environment.ts`, so demo mode); specs for API mode provide `API_MODE`/`API_BASE_URL` themselves.
- `npm start` with `useApi: true` needs the API running: `dotnet run --project CourseAcademy.Api --launch-profile http` in `../CourseAcademy.Backend`.

## Task Workflow (MANDATORY)

For every new task or change request, do NOT start coding right away:
1. **Write a proper prompt first.** Turn the user's request into a clear, structured prompt:
   - Goal (what and why)
   - Scope: affected modules, screens/routes, files (use `docs/business/` to find them)
   - Requirements and business rules (new or changed, with BR-IDs)
   - Out of scope
   - Acceptance criteria (how to check it works)
   - Open questions (must be answered by the user before implementing — no assumptions)
2. **Ask the user to confirm.** Show the prompt and ask: "Shall I implement this?"
   The user may edit it first.
3. **Only after the user says yes, write the code.** Follow the confirmed prompt.
   Then update the docs (see below).

Exception: trivial questions or explanations that change no files need no prompt.

### No assumptions (MANDATORY)
- Never assume anything about business logic, requirements, or technical decisions.
- If anything is unclear, missing, or could be read more than one way: STOP and ask the user first.
- Ask before writing the prompt, and again during coding if a new doubt appears.
- Do not fill gaps with "sensible defaults" or guesses. If something can't be asked right now,
  list it under Open Questions; never write it into docs or code as fact.

## Business Documentation (MANDATORY)

Business logic is documented in `docs/business/`. It is the source of truth
for the domain, database design, and backend.

### Reading (save tokens)
- To understand business logic, read `docs/business/README.md` FIRST,
  then only the relevant `modules/<module>.md`.
- Do NOT read the whole codebase to understand business rules.
  Open source files only when you need to change them.

### Updating (after every task)
After any change that affects business logic, UI flow, fields, validation,
permissions, calculations, or data — before finishing the task:
1. Update the affected `modules/<module>.md` (keep the 10-section structure).
2. Update `data-model.md` and `api-contract.md` if entities/fields/data changed.
3. Update the module map in `README.md` if a module was added or its status changed.
4. Add an entry at the top of `CHANGELOG.md`:
   `YYYY-MM-DD | <module> | <what changed> | <rule IDs affected>`
5. New module → create `modules/<module>.md` using the same 10 sections.
6. Apply the same changes to `APPLICATION-OVERVIEW.md` (the combined whole-app document):
   its Part A summary, the matching Part B module/data-model/API section, and Part C
   (Open Questions, likely bugs). The separate files stay the source of truth.
- Edit only the changed parts. Never rewrite whole files.
- Pure styling/refactoring with no business impact → no doc update needed.
- End every reply with: "Docs updated: <files>" or "Docs: no business change".
