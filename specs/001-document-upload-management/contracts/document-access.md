# HTTP Contract: Authorized Document Access

## Download or preview

**Endpoint**: `GET /documents/file/{documentId}`  
**Authentication**: Required  
**Authorization**: The current user must be the owner, an authorized project manager/member, an active explicit share recipient, or an administrator.

### Request

- `documentId`: positive integer route value.
- Optional `download=1` query value requests attachment disposition; absence requests inline disposition when preview is supported.

### Responses

- `200 OK`: authorized file stream with stored MIME type, safe original filename, and inline/attachment content disposition.
- `400 Bad Request`: invalid document identifier.
- `401 Unauthorized`: no authenticated user; normal application login redirect behavior may apply.
- `404 Not Found`: document does not exist or is not accessible; do not reveal which condition occurred.
- `410 Gone`: optional response for a known deleted document when policy permits; default behavior should remain indistinguishable from inaccessible content.
- `500 Internal Server Error`: storage failure; response must not expose local paths or exception details.

### Security requirements

- Resolve the current user from the authenticated request, never from a user-supplied query parameter.
- Ask the document service for an authorized stream before opening or returning file content.
- Resolve only generated relative paths beneath the configured document storage root; reject traversal or absolute paths.
- Record a Download or Preview activity only after authorization succeeds.
- Do not expose storage paths, internal IDs beyond the route identifier, scanner details, or filesystem errors.

## Upload service contract

The application service accepts a stream plus immutable file metadata and authenticated actor context. It returns a per-file result containing success/failure, document identifier when successful, and user-readable validation errors. It must validate extension/content type/size, authorize project/task context, scan, store, persist, audit, and notify in that order.

## Sharing contract

The application service accepts a document identifier, authenticated actor, and one or more validated user/team recipients. It rejects unauthorized actors and invalid recipients, creates active shares idempotently, records a Share activity, and creates in-app notifications for newly shared recipients.
