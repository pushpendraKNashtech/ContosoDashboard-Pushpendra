# UI Contract: Document Workflows

## Documents page

**Route**: `/documents`  
**Authentication**: Required  
**Purpose**: Browse the current user's documents and documents shared with them.

### Inputs

- Search text: optional; matches title, description, tags, uploader, or project.
- Category: optional controlled category.
- Project: optional authorized project.
- Date range: optional start/end dates.
- Sort: title, upload date, category, or file size; ascending/descending.
- Upload files: one or more files; each file is limited to 25 MB and the supported type allow-list.
- Upload metadata: required title/category, optional description/tags/project/task.

### Outputs and states

- Loading state while authorized results are retrieved.
- Empty state for no documents, no matches, or no shared documents.
- Upload progress per selected file.
- Success result with title, category, size, upload date, and project.
- Validation/error result that identifies unsupported type, size, missing metadata, scan failure, storage failure, or permission denial without exposing paths.
- Actions for preview, download, edit, replace, delete, and share are displayed only when authorized, but service checks remain authoritative.

## Document details page

**Route**: `/documents/{documentId}`  
**Authentication**: Required  
**Purpose**: Display metadata, associations, shares, activity allowed by policy, and lifecycle actions.

- Unauthorized or missing documents must resolve to a safe not-found/denied experience.
- Delete requires explicit confirmation and describes permanent deletion.
- Preview is available only for authorized PDF and image types.
- Metadata edits preserve the document identifier and file association.

## Project details integration

**Route**: `/projects/{projectId}`  
**Authentication**: Required  
**Purpose**: Add an authorized project-document section showing documents associated with the project and an upload action for permitted roles.

- Project members can view/download.
- Project managers can upload/manage project documents.
- Non-members must not receive project document metadata.

## Task details integration

**Route**: `/tasks/{taskId}`  
**Authentication**: Required  
**Purpose**: Show related documents and allow authorized upload/attachment.

- Task access follows existing task authorization rules.
- A document attached to a task inherits or validates the task's project association.

## Dashboard integration

**Route**: `/`  
**Authentication**: Required  
**Purpose**: Add a Recent Documents widget with the current user's five most recent uploads and a document count in the summary.

## Administrator reports

**Route**: `/documents/reports`  
**Authentication**: Required; Administrator role only  
**Purpose**: Display document-type, uploader, and access-pattern reports with an empty state and query failure state.

## Accessibility contract

All document controls must have visible or programmatic labels, keyboard operation, semantic headings/table structure, focusable dialogs, status messages announced through suitable live regions, and error text associated with the invalid field or file.
