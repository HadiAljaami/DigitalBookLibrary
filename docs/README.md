# Digital Book Library — Documentation Index

This folder contains the full analysis and design documentation for the **Digital Book Library API**,
an ASP.NET Core Web API built with **Clean Architecture** (no MediatR / no CQRS).

> Read the documents in order. They move from *what* the system must do → to *how* it is designed.

## Reading Order

| # | Document | Purpose |
|---|----------|---------|
| 01 | [01-SRS.md](01-SRS.md) | Software Requirements Specification — actors, functional & non-functional requirements. |
| 02 | [02-Architecture-Design.md](02-Architecture-Design.md) | Clean Architecture layers, patterns, `ApiResponse`, `Error`/error-codes, exceptions, folder structure. |
| 03 | [03-Data-Model-ERD.md](03-Data-Model-ERD.md) | Entities, relationships and the ERD (Mermaid). |
| 04 | [04-Use-Case-Diagram.md](04-Use-Case-Diagram.md) | Actors and use cases (Mermaid). |
| 05 | [05-API-Contract.md](05-API-Contract.md) | REST endpoint contracts per module (incl. My Library & Admin Dashboard). |
| 06 | [06-Error-Codes.md](06-Error-Codes.md) | Complete catalog of every code the API returns + React i18n snippet. |
| 07 | [07-Testing-Strategy.md](07-Testing-Strategy.md) | xUnit test plan (business rules), tooling, conventions. |

## Key Decisions (locked-in defaults)

| Topic | Decision | Rationale |
|-------|----------|-----------|
| Framework | ASP.NET Core Web API, **.NET 10 (LTS)** | Latest LTS (Nov 2025, supported to 2028). |
| Architecture | Clean Architecture (Domain / Application / Infrastructure / WebAPI) | Testable, SOLID, clear separation of concerns. |
| CQRS / MediatR | **Not used** | Kept deliberately simple; a Services layer instead. |
| Auth | **Custom** JWT + Refresh Token (not ASP.NET Core Identity) | Matches the `UserAccount`/`Role`/`UserRole` model and gives full control over the token/refresh lifecycle. |
| Data access | EF Core + Repository + Unit of Work | UoW is a thin wrapper injected with the EF `DbContext`. |
| EF Core location | **Application has NO EF Core** — all queries in Infrastructure repositories | Repositories return materialized results (`PagedResult<T>`), no `IQueryable` leaks; keeps the Repository a real abstraction and Application infrastructure-free. |
| Mapping | **Manual** (extension methods `ToDto()`) — **no AutoMapper** | Explicit, compile-checked, easy to read and debug. |
| HTTP status | Decided in **WebAPI middleware** by exception type; Application throws HTTP-free semantic exceptions | Keeps transport concerns out of Application (Clean Architecture). |
| Error/message text | Backend returns **codes only**; React translates | `Message` + `Errors[]` are codes; descriptions go to logs only. |
| Database | SQL Server | Widely used across the .NET ecosystem; first-class EF Core support. |
| File storage | Local `wwwroot/uploads` behind `IFileStorageService` | Simple MVP; swappable to cloud; `IFormFile` never leaks into Application. |
| Admin | Dedicated **Dashboard** module (KPIs, charts, user management) | Separates admin analytics/management from the public API surface. |
| Service interfaces | **None** — concrete services, no `IService` | Single implementation each; only cross-boundary **ports** stay interfaces. Still fully testable. |
| Testing | **xUnit + NSubstitute + Shouldly** on critical business rules | Covers rating/download/auth rules. See `07`. |
| API docs | **Swagger (Swashbuckle)** with JWT support | Interactive docs & manual testing. |

## How the diagrams render

All diagrams use **Mermaid**, which renders natively on GitHub and in VS Code (with a Mermaid extension).
No external tools required.
