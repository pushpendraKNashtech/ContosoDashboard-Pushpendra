---

description: "Executable task list for Document Upload and Management"
---

# Tasks: Document Upload and Management

**Input**: Design documents from `specs/001-document-upload-management/`
**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/`, `quickstart.md`

**Organization**: Tasks are grouped by user story so each increment can be implemented and validated independently.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Establish the test project, storage configuration, and shared feature conventions.

- [X] T001 Create the `ContosoDashboard.Tests` project and add its project reference in `ContosoDashboard.Tests/ContosoDashboard.Tests.csproj`
- [X] T002 [P] Add test framework and EF Core test dependencies in `ContosoDashboard.Tests/ContosoDashboard.Tests.csproj`
- [X] T003 [P] Add document storage root and upload limit settings in `ContosoDashboard/appsettings.json`
- [X] T004 [P] Add the document storage directory to `ContosoDashboard/.gitignore`
- [X] T005 [P] Add the document navigation entry and route placeholder in `ContosoDashboard/Shared/NavMenu.razor`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Create shared data, storage, scanning, identity, and authorization foundations required by every user story.

- [X] T006 [P] Add the `Document` entity with required fields, 25 MB size validation, integer key, text category, and navigation properties in `ContosoDashboard/Models/Document.cs`
- [X] T007 [P] Add the `DocumentShare` entity with user/team recipient fields, active state, and sharing metadata in `ContosoDashboard/Models/DocumentShare.cs`
- [X] T008 [P] Add the immutable `DocumentActivity` entity and action values in `ContosoDashboard/Models/DocumentActivity.cs`
- [X] T009 Register document DbSets, relationships, foreign-key delete behavior, unique file-path constraint, and performance indexes in `ContosoDashboard/Data/ApplicationDbContext.cs`
- [X] T010 [P] Add the `IFileStorageService` contract for upload, delete, download, and path-safe stream access in `ContosoDashboard/Services/FileStorageService.cs`
- [X] T011 [P] Implement local filesystem storage outside `wwwroot` with generated relative paths and traversal protection in `ContosoDashboard/Services/LocalFileStorageService.cs`
- [X] T012 [P] Add the `IMalwareScanner` contract and offline scanner implementation boundary in `ContosoDashboard/Services/MalwareScanner.cs`
- [X] T013 Add the document service registration, storage settings binding, and scanner registration in `ContosoDashboard/Program.cs`
- [X] T014 Update login claims to include the authenticated user's department required for team sharing in `ContosoDashboard/Pages/Login.cshtml.cs`
- [X] T015 Add shared document DTOs, upload result types, sort/filter values, and authorization result types in `ContosoDashboard/Services/DocumentContracts.cs`
- [X] T016 Create the document test database factory and seeded authorization fixtures in `ContosoDashboard.Tests/Infrastructure/DocumentTestFixture.cs`

**Checkpoint**: Foundation ready; entities, storage boundaries, authentication context, and test infrastructure are available before story work begins.

---

## Phase 3: User Story 1 - Upload and Organize Documents (Priority: P1) 🎯 MVP

**Goal**: Authenticated employees can upload supported files with required metadata and receive per-file results.

**Independent Test**: Upload a valid supported file as a seeded employee, confirm its metadata and list entry, then verify oversized, unsupported, missing-metadata, and scan-failure cases create no accessible document.

### Tests for User Story 1

- [X] T017 [P] [US1] Test supported extensions, MIME values, 25 MB boundary, required title/category, and invalid metadata in `ContosoDashboard.Tests/Services/DocumentValidationTests.cs`
- [X] T018 [P] [US1] Test unique generated paths, user/project folder layout, traversal rejection, and storage cleanup in `ContosoDashboard.Tests/Services/LocalFileStorageServiceTests.cs`
- [X] T019 [P] [US1] Test upload ordering, scanner rejection, storage failure compensation, database failure compensation, and per-file results in `ContosoDashboard.Tests/Services/DocumentUploadTests.cs`

### Implementation for User Story 1

- [X] T020 [US1] Implement document validation, category allow-list, upload authorization, scan-before-storage, storage/database compensation, metadata persistence, and activity logging in `ContosoDashboard/Services/DocumentService.cs`
- [X] T021 [US1] Add employee upload and My Documents workflow with `InputFile`, metadata form, per-file progress, validation, success, failure, loading, and empty states in `ContosoDashboard/Pages/Documents.razor`
- [X] T022 [US1] Add accessible upload form labels, file status announcements, keyboard-safe controls, and responsive document table styles in `ContosoDashboard/wwwroot/css/site.css`
- [X] T023 [US1] Add document service tests for authenticated employee personal and authorized project uploads in `ContosoDashboard.Tests/Services/DocumentUploadTests.cs`

**Checkpoint**: User Story 1 is independently usable: a user can upload, organize, and see documents with clear validation and results.

---

## Phase 4: User Story 2 - Find and Use Authorized Documents (Priority: P1)

**Goal**: Users can browse, filter, sort, search, preview, and download only documents they are authorized to access.

**Independent Test**: Seed personal, project, and shared documents for multiple users; verify permitted results and streams, then verify an unauthorized user receives no metadata or file content.

### Tests for User Story 2

- [X] T024 [P] [US2] Test owner, project member, project manager, share recipient, administrator, and unauthorized query scopes in `ContosoDashboard.Tests/Authorization/DocumentAuthorizationTests.cs`
- [X] T025 [P] [US2] Test title/category/date/project/file-size sorting and filtering plus bounded authorized search predicates in `ContosoDashboard.Tests/Services/DocumentQueryTests.cs`
- [X] T026 [P] [US2] Test authorized inline preview, attachment download, safe filenames, MIME types, denied access, and missing-document behavior in `ContosoDashboard.Tests/Integration/DocumentAccessEndpointTests.cs`

### Implementation for User Story 2

- [X] T027 [US2] Add authorized document query, sort, filter, search, shared-with-me, and recent-document methods in `ContosoDashboard/Services/DocumentService.cs`
- [X] T028 [US2] Add protected file preview/download endpoint that resolves the current user, requests an authorized stream, prevents path traversal, and returns safe content responses in `ContosoDashboard/Pages/DocumentDownload.cshtml.cs`
- [X] T029 [US2] Add document details, preview, download, search, sort, filter, and shared-document actions in `ContosoDashboard/Pages/DocumentDetails.razor`
- [X] T030 [US2] Complete the documents list page with authorized results, responsive table semantics, empty states, and accessible action labels in `ContosoDashboard/Pages/Documents.razor`
- [X] T031 [US2] Add endpoint mapping and HTTP authorization pipeline support for protected document access in `ContosoDashboard/Program.cs`

**Checkpoint**: User Stories 1 and 2 are independently usable; users can retrieve permitted documents without exposing unauthorized content.

---

## Phase 5: User Story 3 - Manage and Share Documents (Priority: P2)

**Goal**: Owners, project managers, and administrators can perform permitted lifecycle and sharing operations.

**Independent Test**: Exercise edit, replacement, deletion, sharing, and revocation as owner, project manager, team member, administrator, and unauthorized user.

### Tests for User Story 3

- [X] T032 [P] [US3] Test metadata update, replacement validation, permanent deletion, and file cleanup in `ContosoDashboard.Tests/Services/DocumentLifecycleTests.cs`
- [X] T033 [P] [US3] Test user/team shares, duplicate-share idempotency, revocation, recipient notification, and unauthorized sharing in `ContosoDashboard.Tests/Services/DocumentSharingTests.cs`

### Implementation for User Story 3

- [X] T034 [US3] Implement metadata update, replacement, permanent deletion, share, and lifecycle authorization methods in `ContosoDashboard/Services/DocumentService.cs`
- [X] T035 [US3] Add owner lifecycle controls, edit form, share dialog, and deletion action in `ContosoDashboard/Pages/DocumentDetails.razor`
- [X] T036 [US3] Add recipient lookup and department/team selection data through authorized user-service methods in `ContosoDashboard/Services/UserService.cs`
- [X] T037 [US3] Add sharing notification types/messages through `ContosoDashboard/Models/Notification.cs` and `ContosoDashboard/Services/NotificationService.cs`
- [X] T038 [US3] Add shared-with-me navigation and notification-aware document states in `ContosoDashboard/Pages/Documents.razor`

**Checkpoint**: Owners and authorized managers can maintain documents and share them without allowing unauthorized lifecycle changes.

---

## Phase 6: User Story 4 - Use Documents in Existing Workflows (Priority: P2)

**Goal**: Documents are available from project/task workflows and visible in dashboard summaries and notifications.

**Independent Test**: Attach a document from an authorized task/project, verify project inheritance, then verify the dashboard widget, document count, and project notifications.

### Tests for User Story 4

- [X] T039 [P] [US4] Test task visibility, task-project association validation, and authorized task document attachment in `ContosoDashboard.Tests/Integration/TaskDocumentIntegrationTests.cs`
- [X] T040 [P] [US4] Test project document access, project-manager upload permissions, recent five uploads, document count, and project notifications in `ContosoDashboard.Tests/Integration/ProjectDocumentIntegrationTests.cs`

### Implementation for User Story 4

- [X] T041 [US4] Add task document query and attachment/upload orchestration while preserving existing task authorization in `ContosoDashboard/Services/TaskService.cs`
- [X] T042 [US4] Add project document query and project-manager upload integration while preserving project membership authorization in `ContosoDashboard/Services/ProjectService.cs`
- [X] T043 [US4] Add recent five uploads and document count to dashboard summary queries in `ContosoDashboard/Services/DashboardService.cs`
- [X] T044 [US4] Add Recent Documents widget, document count, project links, and accessible empty/loading states in `ContosoDashboard/Pages/Index.razor`
- [X] T045 [US4] Add task detail route and related document list in `ContosoDashboard/Pages/TaskDetails.razor`
- [X] T046 [US4] Add project document list and permitted upload controls to the existing project details page in `ContosoDashboard/Pages/ProjectDetails.razor`
- [X] T047 [US4] Add document navigation entry points in `ContosoDashboard/Shared/NavMenu.razor`

**Checkpoint**: Document workflows are integrated with tasks, projects, dashboard summaries, and notifications.

---

## Phase 7: User Story 5 - Review Document Activity (Priority: P3)

**Goal**: Administrators can review immutable document activity and usage reports while non-administrators are denied.

**Independent Test**: Generate uploads, downloads, previews, replacements, shares, and deletions; verify administrator reports and non-administrator denial.

### Tests for User Story 5

- [X] T048 [P] [US5] Test append-only activity records for every lifecycle action and exclusion of paths, secrets, and file contents in `ContosoDashboard.Tests/Services/DocumentActivityTests.cs`
- [X] T049 [P] [US5] Test administrator-only report access and document type, uploader, and access-pattern aggregation in `ContosoDashboard.Tests/Authorization/DocumentReportAuthorizationTests.cs`

### Implementation for User Story 5

- [X] T050 [US5] Implement immutable activity query and administrator report aggregation methods in `ContosoDashboard/Services/DocumentService.cs`
- [X] T051 [US5] Add administrator-only report route with document type, active uploader, and access-pattern summaries in `ContosoDashboard/Pages/DocumentReports.razor`
- [X] T052 [US5] Add report navigation visibility and safe denied state for non-administrators in `ContosoDashboard/Shared/NavMenu.razor`

**Checkpoint**: Administrators have auditable document reporting and non-administrators cannot access it.

---

## Phase 8: Polish and Cross-Cutting Validation

**Purpose**: Verify the complete feature against its specification, constitution, performance goals, and quickstart.

- [X] T053 [P] Add database query indexes and verify authorized bounded queries for 500-document list/search targets in `ContosoDashboard/Data/ApplicationDbContext.cs` and `ContosoDashboard/Services/DocumentService.cs`
- [X] T054 [P] Add accessibility and responsive document workflow checks to `ContosoDashboard/wwwroot/css/site.css` and `specs/001-document-upload-management/quickstart.md`
- [X] T055 [P] Add structured error logging that excludes file contents, secrets, and local paths in `ContosoDashboard/Services/DocumentService.cs`
- [X] T056 Run the automated document test suite and application build validation from `ContosoDashboard.Tests/ContosoDashboard.Tests.csproj` and `ContosoDashboard/ContosoDashboard.csproj`
- [ ] T057 Execute all manual quickstart scenarios and record results in `specs/001-document-upload-management/quickstart.md`
- [X] T058 Reconcile the `net10.0` project target with the README/package baseline and document the verified supported SDK in `ContosoDashboard/ContosoDashboard.csproj` and `README.md`
- [X] T059 Review implementation against every FR-001 through FR-030 and SC-001 through SC-010 in `specs/001-document-upload-management/spec.md`

---

## Dependencies and Execution Order

### Phase Dependencies

- **Phase 1 Setup**: No dependencies; T001-T005 can begin immediately and parallelize where marked.
- **Phase 2 Foundational**: Depends on Phase 1; T006-T016 establish the shared data, storage, scanner, claims, contracts, and test fixtures.
- **Phase 3 US1**: Depends on Phase 2; delivers the MVP upload and organization workflow.
- **Phase 4 US2**: Depends on Phase 2 and the document persistence contract from US1; browsing and protected file access build on uploaded documents.
- **Phase 5 US3**: Depends on US1 and US2 document/service contracts; lifecycle and sharing build on persisted documents and access rules.
- **Phase 6 US4**: Depends on US1 and US2; integrates document references into existing task, project, dashboard, and notification surfaces.
- **Phase 7 US5**: Depends on activity records from US1-US4; reporting is the final user story.
- **Phase 8 Polish**: Depends on all desired user stories.

### User Story Dependencies

- **US1 (P1)**: Depends only on Foundational; MVP candidate.
- **US2 (P1)**: Depends on Foundational and US1 upload/persistence contracts.
- **US3 (P2)**: Depends on US1 and US2 service/query contracts.
- **US4 (P2)**: Depends on US1 and US2; can begin after the shared document service is stable.
- **US5 (P3)**: Depends on lifecycle activity records from US1-US4.

### Parallel Opportunities

- Phase 1: T002-T005 can run in parallel after T001 establishes the test project.
- Phase 2: T006-T008, T010-T012, and T016 can run in parallel; T009, T013-T015 depend on the relevant contracts/entities.
- US1: T017-T019 can run in parallel before T020; T021-T022 can proceed after service contracts are defined.
- US2: T024-T026 can run in parallel; query, endpoint, and page work can proceed on separate files after service contracts are agreed.
- US3: T032-T033 can run in parallel; lifecycle UI and notification/user lookup work can proceed in parallel after T034's contract is established.
- US4: T039-T040 can run in parallel; task, project, dashboard, and page work can be split by file ownership.
- US5: T048-T049 can run in parallel; report service and report page work can proceed after activity contracts are fixed.
- Different user stories may be staffed in parallel only after their stated dependencies are complete; shared files such as `DocumentService.cs`, `Program.cs`, `ApplicationDbContext.cs`, and `NavMenu.razor` require sequential coordination.

## Parallel Example: MVP

```text
Task T017: Validation tests in ContosoDashboard.Tests/Services/DocumentValidationTests.cs
Task T018: Storage tests in ContosoDashboard.Tests/Services/LocalFileStorageServiceTests.cs
Task T019: Upload orchestration tests in ContosoDashboard.Tests/Services/DocumentUploadTests.cs

After the tests and foundational contracts are ready:
Task T020: Document upload service in ContosoDashboard/Services/DocumentService.cs
Task T021: Upload/list page in ContosoDashboard/Pages/Documents.razor
Task T022: Accessible styles in ContosoDashboard/wwwroot/css/site.css
```

## Implementation Strategy

### MVP First (US1 and minimum retrieval)

1. Complete Phase 1 Setup and Phase 2 Foundational.
2. Complete Phase 3 US1.
3. Complete the minimum US2 query and protected file-access work needed to verify that uploaded documents can be retrieved safely.
4. Run the independent upload, authorization, and cleanup tests plus the first three quickstart scenarios.
5. Stop for review before adding sharing, integrations, or reporting.

### Incremental Delivery

1. Setup and Foundational phases establish secure persistence and storage seams.
2. US1 delivers upload and organization.
3. US2 delivers authorized retrieval and search.
4. US3 delivers lifecycle management and sharing.
5. US4 integrates documents into tasks, projects, dashboard, and notifications.
6. US5 delivers audit reporting.
7. Polish validates performance, accessibility, documentation, and all specification criteria.

## Notes

- Every task uses the required checklist format: checkbox, sequential ID, optional `[P]`, required story label for story tasks, and an exact repository path.
- Tests are included because the approved plan and constitution require repeatable validation for security, data isolation, persistence, and reliability.
- The existing Spec Kit `setup-tasks.ps1` currently resolves `main` instead of `.specify/feature.json`; this task file is located under the approved feature directory and does not modify that infrastructure defect.
