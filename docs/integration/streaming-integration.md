# Streaming Service Integration Spec (v1.0)

Status: **confirmed 2026-11-16**; the Main App side is built (backend Step 6). The former proposals (limits, link lifetimes, webhook window) were accepted as written. Virus scanning and the retry policy are the Streaming service's own concern.

## 1. Systems and responsibilities
| System | Owns | Never does |
|---|---|---|
| **Main App** (ASP.NET Core API, `CourseAcademy.API`) | Users, permissions, courses, enrollments, payments. Decides **who** may upload, watch, download or delete. Stores only the Streaming `assetId` + cached status for each media item. | Store or proxy media bytes; transcode; expose Streaming URLs without an authorization check. |
| **Streaming Service** (NestJS, separate) | Media bytes, temporary upload storage, transcoding (video → HLS), document storage, signed playback/download URLs, asset lifecycle. | Know about users, courses or enrollments; make business decisions. |
| **Angular app** | UI. Calls **only** the Main App API. Exception: uploads file bytes directly to the temporary `uploadUrl` returned by the API. | Call Streaming internal APIs; hold Streaming credentials. |

## 2. Asset kinds
| Kind | Used for | Processing | Delivery |
|---|---|---|---|
| `video` | Section videos | Transcode to HLS (multiple renditions) + thumbnail + duration | Signed HLS playlist URL (short-lived) |
| `document` | Section documents (PDF/DOCX/PPTX), course materials (PDF, Slides, Worksheet) | Virus scan (Streaming side, optional), metadata only | Signed download URL (short-lived) |
Link materials and live-session meeting links are plain external URLs and never go to Streaming.

## 3. Asset status
`uploaded → queued → processing → ready` · any → `failed` · `ready|failed → deleting → deleted`

| Status | Meaning |
|---|---|
| `pending_upload` | Upload created, bytes not yet received (Main App local status only) |
| `uploaded` | Bytes received, upload completed |
| `queued` | Waiting for processing |
| `processing` | Transcoding / scanning |
| `ready` | Playable / downloadable |
| `failed` | Processing failed (`error.code`, `error.message`) |
| `deleting` / `deleted` | Deletion requested / done |

## 4. Upload flow
```
Angular                Main App API                         Streaming
  | POST /api/v1/media/uploads ------>|                                  |
  |  {kind, fileName, mimeType,       |  permission + LMS context check  |
  |   sizeBytes, context}             |  POST /internal/v1/uploads ----->|
  |                                   |<---- {assetId, uploadUrl,        |
  |                                   |       uploadMethod, headers,     |
  |                                   |       expiresAt}                 |
  |<-- {mediaAssetId, assetId,        |  save MediaAsset(pending_upload) |
  |     uploadUrl, headers, expiresAt}|                                  |
  | PUT bytes ------------------------------------------------------>    | (temporary storage)
  | POST /api/v1/media/uploads/{id}/complete ->| POST /internal/v1/uploads/{assetId}/complete -->|
  |                                   |  status = uploaded               |
  | GET /api/v1/media/assets/{id} (poll until ready/failed)              |
  |                                   |<== webhooks: queued, processing, ready|failed
```
- `context` = where the media will be attached, e.g. `{ type: "sectionVideo", courseId, sectionId }`. Main App checks `courses.edit` on that course **before** calling Streaming.
- Upload URL lifetime: **15 minutes** (`Streaming:UploadUrlMinutes`).
- Limits: video ≤ 2 GB, `video/mp4`, `video/webm`, `video/quicktime`; document ≤ 50 MB, `application/pdf`, DOCX, PPTX MIME types. Checked by the Main App before Streaming is called.

## 5. Streaming internal API (called only by Main App)
Base: `{Streaming:BaseUrl}/internal/v1`. Auth header: `X-Api-Key: <secret>` (from configuration, never in source).

| Method | Path | Request | Response |
|---|---|---|---|
| POST | `/uploads` | `{ kind, fileName, mimeType, sizeBytes, externalRef }` | `{ assetId, uploadUrl, uploadMethod: "PUT", headers: {}, expiresAt }` |
| POST | `/uploads/{assetId}/complete` | — | `{ assetId, status }` |
| GET | `/assets/{assetId}` | — | `{ assetId, kind, status, durationSeconds?, sizeBytes, mimeType, thumbnailUrl?, error? }` |
| POST | `/assets/{assetId}/playback` | `{ viewerRef, ttlSeconds }` | `{ playlistUrl, expiresAt }` |
| POST | `/assets/{assetId}/download` | `{ viewerRef, ttlSeconds, fileName }` | `{ downloadUrl, expiresAt }` |
| DELETE | `/assets/{assetId}` | — | `{ assetId, status: "deleting" }` |
`externalRef` = Main App `MediaAsset.Id` (for support/tracing). `viewerRef` = opaque Main App user id (for Streaming logs only).
Playback/download TTL: **10 minutes** (`Streaming:LinkMinutes`).

## 6. Webhooks (Streaming → Main App)
`POST /api/v1/internal/streaming/webhooks`

Headers:
| Header | Value |
|---|---|
| `X-Streaming-Event-Id` | unique event id (UUID) |
| `X-Streaming-Timestamp` | Unix seconds |
| `X-Streaming-Signature` | `sha256=<hex HMAC-SHA256(secret, "{timestamp}.{rawBody}")>` |

Body:
```json
{
  "eventId": "uuid",
  "type": "asset.queued | asset.processing | asset.ready | asset.failed | asset.deleted",
  "occurredAt": "2026-11-14T10:00:00Z",
  "assetId": "string",
  "sequence": 3,
  "data": { "durationSeconds": 612, "sizeBytes": 123456, "mimeType": "video/mp4",
            "thumbnailUrl": "https://...", "error": { "code": "TRANSCODE_FAILED", "message": "..." } }
}
```
Main App rules:
1. Verify signature on the **raw** body with constant-time compare; reject if the timestamp is more than **5 minutes** away from now (`Streaming:WebhookToleranceMinutes`) → `401`.
2. Insert the event into `StreamingWebhookEvent` (unique `EventId`). Duplicate → return `200` without reprocessing.
3. Apply only if `sequence` > the asset's last applied sequence (tolerates out-of-order delivery); older events are stored and ignored.
4. Respond `200` only after the event row is committed. Any failure → `5xx` so Streaming retries.
5. Unknown `assetId` → store and return `200` (asset may have been deleted).

Streaming retry policy (Streaming side): exponential backoff for up to 24 hours on non-2xx.

## 7. Main App rules around assets
- A video/document can be published (visible to students) only when its asset is `ready`.
- Playback/download URL is issued only after the access check: enrolled (active), free preview video, or course grant/admin.
- An asset referenced by any non-deleted curriculum item cannot be deleted; replacing an asset detaches the old one and then requests its deletion.
- Main App never stores Streaming URLs; it requests a fresh signed URL each time.

## 8. Development without the real service
Main App ships a **fake Streaming implementation** (`FakeStreamingClient` + a local fake upload endpoint) selected by configuration `Streaming:Mode = Fake | Http`. It simulates upload, processing delay and webhook delivery.

As built: files go to `App_Data/fake-streaming` (git-ignored); uploads and file links are signed, expiring URLs under `/api/v1/dev/fake-streaming/…`; after `complete` it waits `Streaming:FakeProcessingSeconds` (3) and sends signed `asset.queued → asset.processing → asset.ready` webhooks to the real webhook handler; videos are served as plain files (`format: "file"`), not HLS, and report no duration. The API refuses to start with `Mode = Fake` outside Development.

## 9. Main App implementation notes (backend Step 6)
- Main App API for Angular: see `docs/business/api-contract.md` section 7a. Asset ids of Streaming are never sent to the browser; the browser uses the Main App's `MediaAsset.Id`.
- `pending_upload` is the Main App's own status before `complete`.
- Playback responses carry `format`: `hls` for the real service (`playlistUrl`), `file` for the fake.
- HTTP client: `X-Api-Key` header, standard resilience handler (retries only for safe methods, so `POST /uploads` is never sent twice; timeouts; circuit breaker). 4xx from Streaming → 409 to the browser; 5xx/timeouts → 503.
- If a file stays in progress for more than 30 s without a webhook, `GET /media/assets/{id}` asks Streaming (`GET /assets/{assetId}`) as a fallback.
- Files no lesson or material uses are marked `deleting`; a background worker sends `DELETE /assets/{assetId}` at once and retries every minute; `asset.deleted` sets `deleted`. A deleting/deleted file ignores other events.
- Configuration: `Streaming:Mode`, `BaseUrl`, `ApiKey` and `WebhookSecret` (secrets: user secrets or `Streaming__ApiKey`, `Streaming__WebhookSecret`), `UploadUrlMinutes`, `LinkMinutes`, `WebhookToleranceMinutes`.
