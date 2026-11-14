# Streaming Service Integration Spec (DRAFT v0.1)

Status: **draft for review**. Items marked **(proposal)** need confirmation before Part 5.

## 1. Systems and responsibilities
| System | Owns | Never does |
|---|---|---|
| **Main App** (ASP.NET Core API, `CourseAcademy.Backend`) | Users, permissions, courses, enrollments, payments. Decides **who** may upload, watch, download or delete. Stores only the Streaming `assetId` + cached status for each media item. | Store or proxy media bytes; transcode; expose Streaming URLs without an authorization check. |
| **Streaming Service** (NestJS, separate) | Media bytes, temporary upload storage, transcoding (video → HLS), document storage, signed playback/download URLs, asset lifecycle. | Know about users, courses or enrollments; make business decisions. |
| **Angular app** | UI. Calls **only** the Main App API. Exception: uploads file bytes directly to the temporary `uploadUrl` returned by the API. | Call Streaming internal APIs; hold Streaming credentials. |

## 2. Asset kinds
| Kind | Used for | Processing | Delivery |
|---|---|---|---|
| `video` | Section videos | Transcode to HLS (multiple renditions) + thumbnail + duration | Signed HLS playlist URL (short-lived) |
| `document` | Section documents (PDF/DOCX/PPTX), course materials (PDF, Slides, Worksheet) | Virus scan **(proposal)**, metadata only | Signed download URL (short-lived) |
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
- Upload URL lifetime: **15 minutes (proposal)**.
- Limits **(proposal)**: video ≤ 2 GB, `video/mp4`, `video/webm`, `video/quicktime`; document ≤ 50 MB, `application/pdf`, DOCX, PPTX MIME types.

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
Playback/download TTL: **10 minutes (proposal)**.

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
1. Verify signature on the **raw** body with constant-time compare; reject if timestamp is older than **5 minutes (proposal)** → `401`.
2. Insert the event into `StreamingWebhookEvent` (unique `EventId`). Duplicate → return `200` without reprocessing.
3. Apply only if `sequence` > the asset's last applied sequence (tolerates out-of-order delivery); older events are stored and ignored.
4. Respond `200` only after the event row is committed. Any failure → `5xx` so Streaming retries.
5. Unknown `assetId` → store and return `200` (asset may have been deleted).

Streaming retry policy **(proposal)**: exponential backoff for up to 24 hours on non-2xx.

## 7. Main App rules around assets
- A video/document can be published (visible to students) only when its asset is `ready`.
- Playback/download URL is issued only after the access check: enrolled (active), free preview video, or course grant/admin.
- An asset referenced by any non-deleted curriculum item cannot be deleted; replacing an asset detaches the old one and then requests its deletion.
- Main App never stores Streaming URLs; it requests a fresh signed URL each time.

## 8. Development without the real service
Main App ships a **fake Streaming implementation** (`FakeStreamingClient` + a local fake upload endpoint) selected by configuration `Streaming:Mode = Fake | Http`. It simulates upload, processing delay and webhook delivery.
