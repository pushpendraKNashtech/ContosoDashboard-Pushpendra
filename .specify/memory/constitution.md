<!--
Sync Impact Report
- Version change: unratified template -> 1.0.0
- Modified principles: placeholder principles replaced with eight ContosoDashboard principles
- Added sections: Technology and Training Constraints; Development Workflow and Quality Gates
- Removed sections: none
- Follow-up TODOs: none
-->

# ContosoDashboard Constitution

## Core Principles

### I. Code Quality and Maintainability
Production code MUST use clear, intention-revealing names, nullable-reference-safe C#, focused methods, and consistent formatting. Changes MUST preserve or improve the existing service, model, data, and page boundaries; duplication, hidden side effects, and speculative abstractions MUST be avoided. Rationale: the project is a teaching codebase whose examples must remain readable and maintainable.

### II. Security and Authentication
All protected pages MUST enforce authentication, and every service operation that accepts a user or resource identifier MUST enforce authorization using the requesting user's identity and role. Authorization checks MUST remain in the service layer as defense in depth against IDOR, even when pages use the ASP.NET Core Authorize attribute. The training login MUST remain clearly documented as mock authentication and MUST NOT be represented as production identity security. Rationale: access control is a cross-layer responsibility and the current application intentionally demonstrates claims, roles, cookies, and user isolation.

### III. Data Protection and Compliance
User, project, task, notification, and future document data MUST be accessed only through authorized application paths. Database relationships, validation attributes, unique constraints, and safe deletion behavior MUST be maintained when models change. Sensitive data MUST NOT be written to logs or exposed through unauthorised UI paths. Any future file feature MUST store files outside `wwwroot`, validate type and size, use generated paths, and authorize downloads. Rationale: the application handles personal and work information and documents a future local-to-Azure storage migration.

### IV. Layered Architecture and Separation of Concerns
Blazor pages and Razor Pages MUST own presentation and interaction only. Business rules, authorization decisions, and data access MUST be implemented in the service and data layers; EF Core configuration MUST remain in `ApplicationDbContext`; infrastructure dependencies MUST be accessed through interfaces when a local implementation may later be replaced. Dependency injection MUST be used for application services. Rationale: this matches the existing Models, Services, Data, and Pages structure and preserves the offline-first cloud migration path.

### V. Testing and Reliability
Every new user-visible behavior MUST have a repeatable validation path. Tests MUST cover authentication boundaries, service authorization, user isolation, data validation, and the primary success and failure paths of changed services. Database and integration behavior MUST be exercised when relationships, queries, or persistence rules change. A change MUST NOT be considered complete while build errors, known authorization regressions, or unverified critical paths remain. Rationale: the application is used to teach Spec-Driven Development and security behavior must be demonstrable.

### VI. User Experience and Accessibility
User workflows MUST provide clear loading, empty, success, validation, and failure states. Interactive controls MUST have meaningful labels, keyboard-accessible behavior, appropriate semantic HTML, and visible status/error feedback. Pages MUST preserve responsive Bootstrap-based layouts and must not expose controls for capabilities that are not implemented. Rationale: dashboard users need predictable task, project, notification, and profile workflows, and accessibility is part of reliable functionality.

### VII. Performance and Resource Discipline
Queries MUST filter by the authorized user or resource scope before materializing results, use appropriate indexes and includes, and avoid unnecessary repeated database calls. User-facing list views MUST remain bounded or paged as data grows, and asynchronous I/O MUST be used for database and external resource operations. Performance-sensitive changes MUST state the expected scale and include a validation method. Rationale: the existing context defines indexes for common task, project, notification, and user queries and the application is intended to remain responsive offline.

### VIII. Documentation and Consistency
Feature work MUST begin with a Spec Kit specification and remain traceable through its plan and tasks when the work is more than a trivial correction. README guidance, stakeholder requirements, authorization assumptions, configuration, and known training limitations MUST be updated when behavior changes. New code MUST follow the repository's existing naming, folder, dependency-injection, model-validation, and Razor component conventions. Rationale: documentation is part of the training product and prevents divergence between intended and observable behavior.

## Technology and Training Constraints

The application MUST remain compatible with ASP.NET Core 8, Blazor Server, Razor Pages, EF Core, and SQL Server LocalDB unless a constitution amendment approves a technology change. Core development MUST work offline without cloud service dependencies. Local implementations MUST retain clear interfaces where the documented production migration path targets Azure SQL, Azure Blob Storage, or Microsoft Entra ID. The repository is for training only; production deployment MUST introduce a real identity provider, password protection, MFA, TLS, audit logging, and applicable compliance controls before handling real data.

## Development Workflow and Quality Gates

Feature requests MUST be captured as user-focused specifications with acceptance scenarios and measurable success criteria before implementation. Plans MUST identify affected pages, services, models, data relationships, security boundaries, and validation steps. Tasks MUST be dependency-ordered and traceable to the specification. Reviews MUST verify the constitution, buildability, authorization behavior, data protection, accessibility states, and documentation impact. A reviewer MUST reject a change that weakens security or leaves a required quality gate unverified without an explicitly documented exception.

## Governance

This constitution governs repository development alongside the approved feature specification and implementation plan. When documents conflict, security, data protection, and explicit user requirements take precedence; the conflict and resolution MUST be recorded in the plan or review notes. Amendments MUST update the Sync Impact Report, explain the reason for change, and update affected templates or documentation when necessary. Versioning follows semantic rules: MAJOR for backward-incompatible governance changes, MINOR for new or materially expanded principles, and PATCH for clarifications that do not change obligations. Compliance MUST be reviewed at specification, planning, implementation, and final validation gates.

**Version**: 1.0.0 | **Ratified**: 2026-09-14 | **Last Amended**: 2026-09-14
