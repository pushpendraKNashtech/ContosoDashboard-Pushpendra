# Quickstart: Document Upload and Management

## Prerequisites

- .NET SDK compatible with the current `ContosoDashboard.csproj` target (`net10.0` at planning time).
- SQL Server LocalDB.
- A browser with JavaScript and file upload support.
- PowerShell from the repository root.
- A clean LocalDB database for the first document workflow run.

## Start the application

```powershell
cd ContosoDashboard
dotnet restore
dotnet build
dotnet run
```

Open the HTTPS or HTTP URL printed by the application and sign in at `/login` using one of the seeded training users.

## Scenario 1: Upload and validation

1. Sign in as Ni Kang (`ni.kang@contoso.com`).
2. Open `/documents` and upload a supported PDF or image no larger than 25 MB.
3. Enter a title and category; optionally add tags and the seeded project.
4. Verify per-file progress, a success result, captured size/type/date/uploader, and the document in the list.
5. Repeat with a file larger than 25 MB, an unsupported extension, and missing title/category.
6. Verify each invalid upload is rejected with an actionable error and creates no accessible document.

## Scenario 2: Browse, search, preview, and download

1. On `/documents`, sort by title, upload date, category, and file size.
2. Filter by category, seeded project, and date range.
3. Search by title, description, tag, uploader, and project name.
4. Preview an authorized PDF or image and download it.
5. Verify the returned filename and content type are correct and no local storage path is displayed.

## Scenario 3: Authorization and IDOR protection

1. Upload a personal document as Ni Kang.
2. Sign out and sign in as Floris Kregel or Camille Nicole.
3. Verify the personal document does not appear in unauthorized searches and its file endpoint does not disclose content.
4. Upload a project document as an authorized project member.
5. Verify project members can view/download it, project managers can manage it, and users outside the project cannot access it.
6. Revoke membership or sharing and verify access is removed on the next request.

## Scenario 4: Lifecycle and sharing

1. As the owner, edit metadata and replace the file with another supported file.
2. Share it with a seeded user or team and verify the recipient's Shared with Me view and notification.
3. Confirm deletion and verify both application access and stored content are removed.
4. Attempt the same operations as an unauthorized user and verify the document remains unchanged.

## Scenario 5: Existing workflow integration

1. Open an authorized project details page and verify its document section.
2. Open an authorized task details page, attach or upload a related document, and verify the task/project association.
3. Return to `/` and verify the five most recent uploads and document count.
4. Add a project document and verify relevant project members receive in-app notifications.

## Scenario 6: Administrator reporting

1. Sign in as `admin@contoso.com` and open `/documents/reports`.
2. Verify document type, active uploader, and access-pattern reports.
3. Verify uploads, downloads, previews, replacements, shares, and deletions appear in activity data without file contents or secrets.
4. Sign in as a non-administrator and verify the report route is denied.

## Automated validation

```powershell
dotnet build
 dotnet test
```

The test suite should cover file validation, generated path safety, storage/database compensation, authorization matrix, search scoping, sharing notifications, audit records, and report access. Browser validation should cover the scenarios above because the existing project is a Blazor Server application and currently has no established end-to-end test harness.

## Accessibility and responsive checks

- Every upload, search, filter, replacement, sharing, preview, download, and delete control has a visible or programmatic label.
- Keyboard-only navigation can reach the file picker, metadata fields, actions, dialogs, and status messages in a logical order.
- Upload success, validation failures, and permission failures are visible and announced through status text without relying on color alone.
- Document tables remain usable at mobile widths and expose a caption or equivalent accessible name.
- Empty, loading, and error states remain readable at desktop and mobile widths.

## Clean-state recovery

If a failed development upload leaves inconsistent local data, stop LocalDB and recreate the training database before rerunning the scenarios. Use the repository's documented LocalDB reset procedure, then restart the application so `EnsureCreated` can initialize the schema and seed data.
