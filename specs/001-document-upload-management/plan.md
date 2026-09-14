# Implementation Plan: Document Upload and Management

**Branch**: `001-document-upload-management` | **Date**: 2026-09-14 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `specs/001-document-upload-management/spec.md`

## Summary

Add secure document upload, browsing, search, preview, download, sharing, lifecycle management, task/project integration, dashboard visibility, notifications, and administrator audit reporting. Implement the feature inside the existing Blazor Server, Razor Pages, EF Core, and service-layer architecture. Keep files outside `wwwroot`, use generated relative paths and an `IFileStorageService` abstraction, enforce authorization in the service layer, and preserve an offline local-filesystem implementation with a future Azure Blob replacement point.

## Technical Context

**Language/Version**: C# with the repository's current `net10.0` target; README and package references identify an older .NET 8 baseline and must be reconciled during implementation validation.  
**Primary Dependencies**: ASP.NET Core Blazor Server, Razor Pages, EF Core SQL Server provider, Bootstrap 5.3/Bootstrap Icons, `IBrowserFile`/`InputFile`, and dependency injection. No cloud SDK is required for the training implementation.  
**Storage**: SQL Server LocalDB for document metadata and local filesystem storage outside `wwwroot` for file content; storage access is abstracted for future Azure Blob Storage.  
**Testing**: Add focused unit/integration coverage for document services and authorization; use `dotnet build` plus the runnable scenarios in `quickstart.md` for Blazor, filesystem, and browser workflow validation.  
**Target Platform**: Offline-capable web application hosted by ASP.NET Core with a desktop browser client.
**Project Type**: Single web project with Blazor Server UI, Razor Pages for HTTP endpoints, EF Core data access, and application services.  
**Performance Goals**: Upload supported files up to 25 MB within 30 seconds in typical conditions; document lists and searches within 2 seconds for 500 documents; previews within 3 seconds.  
**Constraints**: Files must remain outside `wwwroot`; only approved file types are accepted; maximum 25 MB per file; authorization must be enforced before metadata or file access; offline training must not depend on cloud services; no soft delete, version history, or external document integrations.  
**Scale/Scope**: Existing seeded users/projects plus up to 500 authorized documents per list view; five user stories, seven document-related entities/relationships, new document pages, HTTP file access, dashboard/task/project/notification integration, and administrator reporting.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

**Status: PASS**

- Code quality: follows the existing Models, Services, Data, Pages, and Razor Pages boundaries; generated paths and validation are centralized rather than duplicated in UI components.
- Security: all document queries and file endpoints receive the requesting user identity; ownership, project membership, sharing, role, and administrator checks remain in the service layer; files are never served from `wwwroot`.
- Data protection: metadata uses validated fields and integer keys consistent with the existing model; file paths are generated and relative; failed storage/database operations are compensated so no usable orphan record remains.
- Architecture: local storage and malware scanning use interfaces so the business service does not depend on filesystem or future cloud implementations.
- Testing: authorization matrices, validation boundaries, persistence/storage sequencing, and primary UI flows have explicit checks in the plan and quickstart.
- UX/accessibility: pages provide labels, keyboard-accessible controls, loading/empty/error/success states, progress, and confirmation before permanent deletion.
- Performance: authorized filtering happens before materialization; indexes and bounded results support the 500-document and two-second goals.
- Documentation: this plan, its research, data model, contracts, and quickstart remain traceable to the feature specification.

## Project Structure

### Documentation (this feature)

```text
specs/001-document-upload-management/
├── plan.md              # This file
├── research.md          # Phase 0 decisions
├── data-model.md        # Phase 1 entities and relationships
├── quickstart.md        # Phase 1 validation guide
├── contracts/           # Phase 1 UI and HTTP contracts
└── tasks.md             # Phase 2 output from /speckit.tasks
```

### Source Code (repository root)

```text
ContosoDashboard/
├── Data/ApplicationDbContext.cs
├── Models/
│   ├── Document.cs
│   ├── DocumentShare.cs
│   └── DocumentActivity.cs
├── Services/
│   ├── DocumentService.cs
│   ├── FileStorageService.cs
│   ├── MalwareScanner.cs
│   └── DashboardService.cs / NotificationService.cs / ProjectService.cs / TaskService.cs
├── Pages/
│   ├── Documents.razor
│   ├── DocumentDetails.razor
│   ├── DocumentReports.razor
│   ├── TaskDetails.razor
│   ├── ProjectDetails.razor
│   └── DocumentDownload.cshtml.cs or an equivalent authorized endpoint
├── Program.cs
└── appsettings.json

ContosoDashboard.Tests/
├── Services/DocumentServiceTests.cs
├── Services/FileStorageServiceTests.cs
├── Authorization/DocumentAuthorizationTests.cs
└── Integration/DocumentWorkflowTests.cs
```

**Structure Decision**: Extend the existing single ASP.NET Core project in place. Models define persisted document metadata and audit relationships; services own validation, authorization, storage orchestration, notifications, and reporting; Blazor pages provide user workflows; a Razor Page or equivalent HTTP endpoint streams authorized files because content remains outside `wwwroot`. Add a separate test project because the repository currently has no test project and the feature's security and filesystem boundaries need isolated coverage.

## Complexity Tracking

No constitution violations. The design uses the existing application project plus one focused test project; no additional production project or repository abstraction is required.
