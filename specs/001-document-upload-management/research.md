# Research: Document Upload and Management

## Decision: Keep document metadata in EF Core and file content in local storage outside `wwwroot`

**Rationale**: The current application uses EF Core with SQL Server LocalDB for relational entities and runs offline. Keeping metadata in the existing database supports project, task, user, notification, and audit relationships. Keeping content outside `wwwroot` prevents direct unauthorised static access and gives the application an authorization point before download or preview.

**Alternatives considered**:
- Store files directly in the database: rejected because it increases database payload size and does not match the repository's documented local-filesystem/cloud-storage migration path.
- Store files under `wwwroot`: rejected because static-file middleware could bypass authorization.
- Use Azure Blob Storage immediately: rejected because the feature must work offline and without cloud dependencies.

## Decision: Introduce `IFileStorageService` with a local filesystem implementation

**Rationale**: The stakeholder requirements explicitly require a storage abstraction and a future Azure Blob implementation. The application already uses dependency injection and service interfaces. The implementation should store relative paths using `{userId}/{projectId or personal}/{guid}.{extension}`, create the directory before saving, and never use the user-provided filename as a path.

**Alternatives considered**:
- Put `System.IO` calls in the Blazor page: rejected because it violates the layered architecture and makes authorization, cleanup, and future migration difficult.
- Use a repository abstraction for every entity: rejected because the existing application accesses EF Core through focused services and does not need a broad repository layer for this feature.

## Decision: Make upload orchestration fail closed and compensate partial work

**Rationale**: The required sequence is validate file, authorize context, generate a unique path, scan the stream, save content, persist metadata, then notify. If database persistence fails after storage succeeds, the service must delete the stored file. If storage fails, it must not persist metadata. A scanner abstraction allows the offline implementation to use a configured local scanner or an explicit training-safe validation adapter while keeping the business contract fail closed when scanning is unavailable.

**Alternatives considered**:
- Persist metadata before saving the file: rejected because failed storage creates orphan records and empty paths.
- Treat scanning as optional: rejected because the specification requires no file to become accessible before scanning succeeds.
- Depend directly on a cloud antivirus service: rejected because the training application must remain offline.

## Decision: Enforce access through a centralized document service and a protected HTTP stream endpoint

**Rationale**: Blazor pages can request authorized metadata and invoke service operations, but files outside `wwwroot` need an HTTP response for preview/download. The endpoint must resolve the current authenticated user, ask the document service for an authorized stream, and return 404/403 without revealing whether an inaccessible document exists. The service owns owner, project manager, project member, shared-recipient, and administrator rules.

**Alternatives considered**:
- Return permanent file URLs: rejected because URLs could be shared or accessed without a fresh authorization check.
- Let the page read arbitrary filesystem paths: rejected because it creates path traversal and authorization risks.
- Put all permissions in UI conditionals: rejected because direct requests and service callers would bypass the checks.

## Decision: Use text categories and integer document keys

**Rationale**: The stakeholder requirements require integer `DocumentId` values consistent with existing entities and text category values for simplicity. Categories are validated against a fixed list in the service and represented as user-facing text.

**Alternatives considered**:
- GUID document keys: rejected because they diverge from the existing User, Project, and Task key style.
- Integer category enums: rejected because the requirements explicitly call for text storage and future category extensibility.

## Decision: Use bounded, authorized query composition for browsing and search

**Rationale**: The performance goals target 500 documents and two-second list/search responses. Queries should apply permission scope, filters, and search predicates before materialization, use indexes on owner/project/upload date/category, and cap result sets or use paging. Search fields are title, description, tags, uploader display name, and project name.

**Alternatives considered**:
- Load all documents and filter in the Blazor component: rejected because it leaks data into the client and scales poorly.
- Add a separate search engine: rejected because the training scope is LocalDB/offline and the expected scale is modest.

## Decision: Extend existing notification and dashboard services rather than create parallel mechanisms

**Rationale**: Notifications already have user ownership, read state, priority, and a service API. Dashboard summaries and recent items already flow through `DashboardService` and the home page. Document events should use those existing contracts and preserve user isolation.

**Alternatives considered**:
- Add a second notification store: rejected because it would split unread counts and user experience.
- Poll a separate document feed: rejected because it duplicates dashboard querying and state management.

## Decision: Validate with focused automated tests plus the repository's runnable workflow

**Rationale**: The repository currently has no test project, while the constitution requires repeatable validation for authorization, data isolation, and changed persistence behavior. A focused test project should cover service and storage seams; `dotnet build` and the quickstart scenarios should cover startup, LocalDB, browser upload, and protected streaming.

**Alternatives considered**:
- Rely only on manual browser testing: rejected because permission and cleanup edge cases need deterministic coverage.
- Add broad end-to-end infrastructure first: rejected because it would delay the highest-risk service and authorization checks.
