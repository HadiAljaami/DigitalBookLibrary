# Digital Book Library API

A production-style **ASP.NET Core Web API** for a digital book library — catalogue, reading &
downloading, ratings, threaded comments, saved lists, and a full admin dashboard with an audit
trail. Built on **.NET 10** with **Clean Architecture**, custom JWT auth, and EF Core.

![CI](https://github.com/HadiAljaami/DigitalBookLibrary/actions/workflows/ci.yml/badge.svg)

---

## Features

- **Auth** — registration, login, JWT access tokens with **rotating refresh tokens** (stored hashed, revocable), role-based authorization (Admin / Member).
- **Catalogue** — books, authors, and a self-referencing **category tree**; paged/filtered/sorted listings.
- **Reading & files** — read-online and download endpoints that stream the PDF, each logged; per-book download/read counters kept consistent with their logs.
- **Interaction** — 1–5 ratings (one per user, re-rating updates), threaded comments (owner/admin moderation), and saved/favourite books (idempotent).
- **Admin dashboard** — KPI totals, top books, recent-activity feed, gap-free time series and distributions for charts, plus user management (activate/deactivate, role assignment).
- **Auditing** — every create/update/delete of an audited entity is recorded (old/new values, actor, IP) via an EF `SaveChanges` interceptor; secrets are stripped from the trail.
- **Hardening** — rate-limited auth endpoints, configurable CORS, consistent `ApiResponse` envelope, and centralized exception→HTTP mapping.

## Architecture

Clean Architecture — dependencies point inward, and the Application layer is **HTTP- and EF-free**:

```
Domain          entities, value objects, domain errors, port interfaces  (no dependencies)
Application     services (business rules), DTOs, manual mapping, validation
Infrastructure  EF Core DbContext, repositories, JWT, password hashing, file storage, auditing
WebAPI          controllers, middleware, DI wiring, Swagger  (composition root)
```

- **No MediatR, no CQRS, no AutoMapper** — services are plain classes; mapping is manual extension methods.
- Services expose no interfaces (single implementation each) yet stay fully unit-testable by substituting their **ports** (`IRepository<T>`, `IUnitOfWork`, `IJwtProvider`, …).
- Clients only ever receive **stable error codes**; human-readable descriptions go to logs.

## Tech stack

| Area | Choice |
|------|--------|
| Runtime | .NET 10 (LTS) |
| Web | ASP.NET Core Web API + Swagger (Swashbuckle) |
| Data | EF Core 10 · SQL Server (LocalDB in dev) |
| Auth | Custom JWT + refresh-token rotation, PBKDF2 password hashing |
| Validation | FluentValidation |
| Tests | xUnit · NSubstitute · Shouldly |

---

## Getting started

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server **LocalDB** (ships with Visual Studio) or any SQL Server instance

### Run

```bash
git clone https://github.com/HadiAljaami/DigitalBookLibrary.git
cd DigitalBookLibrary

# (optional) override dev secrets locally — see Configuration below
dotnet user-secrets --project src/DigitalBookLibrary.WebAPI set "Jwt:SecretKey" "<a-long-random-32+char-string>"

dotnet run --project src/DigitalBookLibrary.WebAPI
```

In **Development**, the app applies EF migrations and seeds baseline data (roles + an admin
account) on startup, so a fresh clone is runnable immediately. Then open Swagger:

```
http://localhost:5014/swagger
```

**Seeded admin (dev default):** `admin@digitalbooklibrary.local` / `Admin#12345` — change it via
configuration for anything beyond local use.

There is also a ready-to-run request collection at
[`src/DigitalBookLibrary.WebAPI/DigitalBookLibrary.WebAPI.http`](src/DigitalBookLibrary.WebAPI/DigitalBookLibrary.WebAPI.http)
(VS Code REST Client / Visual Studio) — run **login** first and the token flows into the rest.

### Configuration

Settings live in `appsettings.json` with **dev-only placeholders**. Override anything sensitive via
`dotnet user-secrets` (dev) or environment variables (production) — never commit real values.

| Key | Purpose | Override in prod? |
|-----|---------|-------------------|
| `ConnectionStrings:DefaultConnection` | SQL Server connection | **Yes** |
| `Jwt:SecretKey` | JWT signing key (≥ 32 chars) | **Yes** |
| `Seed:AdminPassword` | Seeded admin password | **Yes** |
| `Cors:AllowedOrigins` | Front-end origins allowed by CORS | Yes |
| `Jwt:AccessTokenMinutes` / `RefreshTokenDays` | Token lifetimes | Optional |

Environment-variable form uses `__` for nesting, e.g. `Jwt__SecretKey`, `ConnectionStrings__DefaultConnection`.

### Migrations

The design-time factory lives in Infrastructure, so run EF tools with it as **both** project and
startup project:

```bash
dotnet ef migrations add <Name> \
  --project src/DigitalBookLibrary.Infrastructure \
  --startup-project src/DigitalBookLibrary.Infrastructure
```

---

## Testing

```bash
dotnet test
```

25 unit tests cover the critical business rules (T-1…T-10 in
[`docs/07-Testing-Strategy.md`](docs/07-Testing-Strategy.md)) — rating rules, download
availability, password hashing, refresh-token rotation, category-delete guards, saved-book
idempotency, and comment ownership. CI runs restore → build → test on every push and PR.

## Project structure

```
src/
  DigitalBookLibrary.Domain/          entities, errors, ports
  DigitalBookLibrary.Application/     services, DTOs, mapping, validation
  DigitalBookLibrary.Infrastructure/  EF Core, repositories, JWT, hashing, files, auditing
  DigitalBookLibrary.WebAPI/          controllers, middleware, Program.cs, Swagger
tests/
  DigitalBookLibrary.UnitTests/       xUnit + NSubstitute + Shouldly
docs/                                 SRS, ERD, use cases, API contract, error codes, testing
```

Full analysis and design docs are in [`docs/`](docs/). The API contract is
[`docs/05-API-Contract.md`](docs/05-API-Contract.md); error codes are
[`docs/06-Error-Codes.md`](docs/06-Error-Codes.md).

## Status

The backend is feature-complete and unit-tested: catalogue, authentication, reading/downloading,
ratings, comments, saved lists, auditing, and the admin dashboard. A web front-end and cloud
deployment are in progress.
