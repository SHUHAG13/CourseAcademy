# Security review — 2026-11-17

Scope: the ASP.NET Core API (`CourseAcademy.API`) and the Angular app (`CourseAcademy.UI`) in API mode, after backend Steps 1–6.

## Checked and in order
| Area | Finding |
|---|---|
| Packages | `dotnet list package --vulnerable --include-transitive`: none. `npm audit --omit=dev`: 0 vulnerabilities. |
| Authentication | JWT (HS256, 15 min) with only the user id; permissions read from the database per request. Refresh tokens are random, stored as SHA-256 hashes, single use; reuse ends all sessions. Sign-in, sign-up and refresh are rate limited (20/min per address). |
| Authorization | Every endpoint that is not public has `[Authorize]`; permission and course checks are in the services and covered by unit tests. Public endpoints: sign-in/up/refresh, catalogue, instructors, lookups, health, playback/download (access checked in the service: preview items only), payment callback and Streaming webhooks (signature verified), fake Streaming (Development only, signed expiring links). |
| Secrets | JWT key, Streaming API key and webhook secret come from user secrets or environment variables; none in the repository. The Development connection string uses Windows authentication (no password). |
| Errors and logs | Unexpected errors return a generic message; details are only logged. Requests the client cancelled are not logged as errors. Request logs contain method, path and status, not bodies or query strings. |
| HTTP | Added: `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: no-referrer` on every response. HTTPS redirection and HSTS outside Development. CORS limited to the configured origins. |
| Payments | Amount always taken from the course on the server; demo gateway refused outside Development; callbacks verified by the gateway plug-in. |
| Media | Upload type and size checked before Streaming is called; Streaming ids and file URLs never stored or sent to the browser (short-lived links only, after an access check); webhooks HMAC-SHA256 signed, 5-minute window, de-duplicated, applied in order. The upload request to storage never carries the sign-in token. |
| Frontend | No `innerHTML`; the only trusted resource URL is the YouTube/Vimeo embed address the player rebuilds from a video id. `returnUrl` after sign-in goes through the Angular router, so it can only open a page of the app. |

## Open points (decisions, not bugs)
- Tokens are kept in `localStorage` (Remember me) or `sessionStorage`, as decided in backend Step 2. A script injected into the page could read them; httpOnly cookies would avoid that but need CSRF protection and changes on both sides.
- No Content-Security-Policy header yet; it belongs on the web server that hosts the Angular app (it must allow the API, the Streaming CDN and YouTube/Vimeo frames).
- Rate limiting covers only sign-in, sign-up and refresh. Uploads and other writes are limited only by permissions.
