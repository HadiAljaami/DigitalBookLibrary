# Architecture & Design
## Digital Book Library API

This document defines the technical design: layering, dependency rules, cross-cutting patterns
(`ApiResponse`, `Error`/error codes, exceptions, middleware), and the physical project/folder structure.

---

## 1. Clean Architecture Overview

Four projects. **Dependencies point inward** — outer layers depend on inner, never the reverse.

```
┌─────────────────────────────────────────────────────────────┐
│                        WebAPI (Presentation)                 │
│  Controllers · Middleware · Program.cs · Swagger · DI root   │
│        depends on ▸ Application, Infrastructure              │
└───────────────┬──────────────────────────┬──────────────────┘
                │                          │
                ▼                          ▼
┌───────────────────────────┐  ┌───────────────────────────────┐
│      Application           │  │        Infrastructure         │
│  Services · DTOs · Mapping │  │  DbContext · Repositories     │
│  Interfaces (contracts)    │◀─│  UnitOfWork · JWT · Files     │
│  ApiResponse · Validation  │  │  implements Application/Domain │
│     depends on ▸ Domain    │  │       interfaces              │
└─────────────┬──────────────┘  └───────────────┬───────────────┘
              │                                 │
              ▼                                 ▼
        ┌───────────────────────────────────────────┐
        │                 Domain                     │
        │  Entities · Errors · Exceptions            │
        │  Repository/UoW interfaces · Enums         │
        │        depends on ▸ nothing                │
        └───────────────────────────────────────────┘
```

### Dependency rules
- **Domain** references nothing external (pure C#). No EF Core, no ASP.NET.
- **Application** references Domain only. Defines service logic and DTOs; declares the contracts it needs.
- **Infrastructure** references Application + Domain. Implements persistence, JWT, file storage.
- **WebAPI** references Application + Infrastructure. Wires everything via DI; owns HTTP concerns.

> Interfaces live where they are **consumed**, not where they are implemented:
> `IRepository<T>` and `IUnitOfWork` are declared in **Domain** (used by Application services),
> and implemented in **Infrastructure**.

---

## 2. Patterns Used

| Pattern | Where | Notes |
|---------|-------|-------|
| **Repository** | `IRepository<T>` (Domain) → `Repository<T>` (Infra) | Generic base + specific repos where custom queries are needed. |
| **Unit of Work** | `IUnitOfWork` (Domain) → `UnitOfWork` (Infra) | Thin wrapper over `DbContext`; exposes `SaveChangesAsync`. Repositories share the same context instance. |
| **Service layer** | Application | Replaces MediatR/CQRS. One service per aggregate/module. |
| **DTO + manual mapping** | Application | No entity ever crosses the API boundary. Mapping via **extension methods** (`ToDto()`), no AutoMapper. |
| **Result envelope** | `ApiResponse<T>` | Single response shape. |
| **Structured errors** | `Error` (Code + Description) | Code → client, Description → logs. |
| **Global middleware** | WebAPI | Central exception → `ApiResponse` translation. |
| **Options pattern** | `JwtOptions`, `FileStorageOptions` | Strongly-typed config. |

### 2.1 Unit of Work contract (as requested)
```csharp
// Domain/Interfaces/IUnitOfWork.cs
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```
```csharp
// Infrastructure/Persistence/UnitOfWork.cs
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    public UnitOfWork(AppDbContext context) => _context = context;

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => _context.SaveChangesAsync(ct);
}
```
Repositories are injected with the **same** `AppDbContext` (scoped), so `SaveChangesAsync` commits all
tracked changes together — one logical transaction per request.

### 2.2 Data-access decision — **Application has NO EF Core** (repositories are a real abstraction)

We deliberately do **not** expose `IQueryable` from repositories and do **not** reference EF Core in Application.
Rationale: exposing `IQueryable`/`DbSet` would make the Repository a redundant wrapper (DbSet already *is*
IQueryable + repository/UoW), whereas the project explicitly wants a meaningful Repository + UoW. Keeping EF Core
out of Application mirrors the same discipline we apply to HTTP, and gives a clean "Application has zero
infrastructure dependencies" story.

Consequences:
- **Generic base** covers CRUD + EF-free predicate helpers (`Expression<Func<T,bool>>` is `System.Linq.Expressions`, not EF).
- **Complex reads** (Include / projection / paging / aggregates) live in **entity-specific repositories** that return
  **materialized** results (`PagedResult<T>`, entity-with-includes). All EF usage is confined to Infrastructure.
- A single reusable `ToPagedResultAsync(page, size)` helper (Infrastructure) removes paging boilerplate.

```csharp
// Domain/Interfaces/IRepository.cs  — no IQueryable, no EF types
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<T>> ListAsync(CancellationToken ct = default);
    Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default);
    Task AddAsync(T entity, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<T> entities, CancellationToken ct = default);
    void Update(T entity);
    void Remove(T entity);
}

// Domain/Interfaces/IBookRepository.cs  — entity-specific, returns materialized results
public interface IBookRepository : IRepository<Book>
{
    Task<PagedResult<Book>> GetPagedAsync(BookQueryOptions options, bool includeHidden, CancellationToken ct = default);
    Task<Book?> GetWithDetailsAsync(int id, CancellationToken ct = default);   // Author + Category
}
```
> `PagedResult<T>` and `PaginationParams` live in **Domain/Common** so repository interfaces can return them.
> Entity query-option records (e.g. `BookQueryOptions : PaginationParams`) carry the filter/sort fields.

---

## 3. The `ApiResponse<T>` Envelope

Every endpoint returns this shape. **Both `Message` and `Errors` carry stable codes — never human sentences.**
The frontend owns all translation; the server never ships display text. Human-readable descriptions exist only
in the `Error` catalog and are written to the **logger** (see §4).

```csharp
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;   // a stable CODE, e.g. "SUCCESS" / "OPERATION_FAILED"
    public T? Data { get; set; }
    public List<string> Errors { get; set; } = new();     // error CODES only

    public static ApiResponse<T> Ok(T data, string code = "SUCCESS")
        => new() { Success = true, Message = code, Data = data };

    public static ApiResponse<T> Fail(string code, params string[] errorCodes)
        => new() { Success = false, Message = code, Errors = errorCodes.ToList() };
}
```
- `Message` is a **code key** (e.g. `SUCCESS`, `BOOK_CREATED`, `OPERATION_FAILED`) the frontend maps to localized text — not a sentence.
- A non-generic `ApiResponse` (or `ApiResponse<object>`) is used for responses without a payload.
- HTTP status code is set by the controller/middleware; the envelope is always present.
- **Nothing human-readable ever leaves the server**: no `ex.Message`, no stack trace, no `Error.Description`.

### Example success
```json
{ "success": true, "message": "SUCCESS", "data": { "id": 12, "title": "Clean Code" }, "errors": [] }
```
### Example failure (only codes leak to client)
```json
{ "success": false, "message": "OPERATION_FAILED", "data": null, "errors": ["BOOK_NOT_FOUND"] }
```

---

## 4. Structured Errors & Logging Separation

The core security requirement: **codes to the client, descriptions to the logs.**

```csharp
// Domain/Errors/Error.cs
public sealed record Error(string Code, string Description)
{
    public static readonly Error None = new(string.Empty, string.Empty);
}
```
- `Code` — stable, unique key, `UPPER_SNAKE_CASE` (e.g. `BOOK_OUT_OF_STOCK`). **Sent to the frontend.**
- `Description` — rich technical detail for developers. **Written to logs only, never returned.**

### 4.1 Error catalog — nested static classes per entity
```csharp
// Domain/Errors/BookErrors.cs
public static class BookErrors
{
    public static readonly Error NotFound =
        new("BOOK_NOT_FOUND", "No book exists with the supplied identifier.");
    public static readonly Error NotAvailable =
        new("BOOK_NOT_AVAILABLE", "The book is marked unavailable and cannot be downloaded.");
    public static readonly Error FileMissing =
        new("BOOK_FILE_MISSING", "The book has no PDF file associated with it.");
}

public static class UserErrors
{
    public static readonly Error NotFound       = new("USER_NOT_FOUND", "...");
    public static readonly Error EmailInUse     = new("USER_EMAIL_IN_USE", "...");
    public static readonly Error InvalidCredentials = new("AUTH_INVALID_CREDENTIALS", "...");
    public static readonly Error Inactive       = new("USER_INACTIVE", "...");
}
// RatingErrors, CommentErrors, CategoryErrors, AuthorErrors, AuthErrors ...
```

### 4.2 Error code naming convention
`<DOMAIN>_<CONDITION>` — e.g. `BOOK_NOT_FOUND`, `RATING_OUT_OF_RANGE`, `AUTH_REFRESH_TOKEN_REVOKED`.
Keep them unique across the whole system so the frontend dictionary has no collisions.

---

## 5. Custom Exceptions

> **Design rule (important):** exceptions in Domain/Application are **transport-agnostic** — they carry the
> semantic `Error` (code + description) but **NO HTTP status code**. HTTP is a WebAPI concern; the mapping
> *exception type → HTTP status* lives only in the middleware (§6). The Application layer never references HTTP.

Each exception **type** expresses a category of failure. The type — not a number — is what the middleware maps.

```csharp
// Domain/Exceptions/AppException.cs  — no HTTP anywhere here
public abstract class AppException : Exception
{
    public Error Error { get; }
    protected AppException(Error error) : base(error.Description) => Error = error;
}

public sealed class NotFoundException      : AppException { public NotFoundException(Error e)      : base(e) {} }
public sealed class ConflictException      : AppException { public ConflictException(Error e)      : base(e) {} }
public sealed class UnauthorizedAppException: AppException { public UnauthorizedAppException(Error e): base(e) {} }
public sealed class ForbiddenException     : AppException { public ForbiddenException(Error e)      : base(e) {} }

// Validation can carry several errors at once
public sealed class ValidationAppException : AppException
{
    public IReadOnlyList<Error> Errors { get; }
    public ValidationAppException(IEnumerable<Error> errors)
        : base(new Error("VALIDATION_FAILED", "One or more validation rules failed."))
        => Errors = errors.ToList();
}
```
Services throw these; controllers stay thin and don't try/catch business errors.
> Note `AppException` lives in **Domain** — it depends on nothing external, which is why it is HTTP-free by construction.

---

## 6. Global Exception Middleware  (the ONLY place that knows HTTP)

The middleware lives in **WebAPI**. It maps each exception **type → HTTP status**, logs the technical
description, and returns an `ApiResponse` with **codes only**.

```csharp
// WebAPI/Middleware/GlobalExceptionMiddleware.cs
public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        => (_next, _logger) = (next, logger);

    // exception TYPE -> HTTP status. HTTP knowledge is confined here, in the WebAPI layer.
    private static int MapStatus(AppException ex) => ex switch
    {
        NotFoundException        => StatusCodes.Status404NotFound,
        ValidationAppException   => StatusCodes.Status400BadRequest,
        ConflictException        => StatusCodes.Status409Conflict,
        UnauthorizedAppException => StatusCodes.Status401Unauthorized,
        ForbiddenException       => StatusCodes.Status403Forbidden,
        _                        => StatusCodes.Status400BadRequest
    };

    public async Task Invoke(HttpContext ctx)
    {
        try { await _next(ctx); }
        catch (ValidationAppException ex)
        {
            _logger.LogWarning("Validation failed: {Codes}", string.Join(",", ex.Errors.Select(e => e.Code)));
            await WriteAsync(ctx, MapStatus(ex),
                ApiResponse<object>.Fail("VALIDATION_FAILED", ex.Errors.Select(e => e.Code).ToArray()));
        }
        catch (AppException ex)
        {
            // log the DESCRIPTION (developer detail); return only the CODE
            _logger.LogWarning("Handled {Type} {Code}: {Description}",
                ex.GetType().Name, ex.Error.Code, ex.Error.Description);
            await WriteAsync(ctx, MapStatus(ex),
                ApiResponse<object>.Fail("OPERATION_FAILED", ex.Error.Code));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");           // full stack to logs only
            await WriteAsync(ctx, StatusCodes.Status500InternalServerError,
                ApiResponse<object>.Fail("INTERNAL_SERVER_ERROR", "INTERNAL_SERVER_ERROR"));
        }
    }
}
```
**Rule:** the client never sees `ex.Message`/stack traces/`Description`; only the code(s).

---

## 7. Pagination, Filtering, Sorting, Searching

`PaginationParams` and `PagedResult<T>` live in **Domain/Common** (so repository interfaces can return them).
Entity query-option records extend `PaginationParams` with their filter/sort fields.

```csharp
// Domain/Common — PageSize clamped to MaxPageSize to prevent unbounded queries
public class PaginationParams
{
    public const int MaxPageSize = 50;
    private int _pageNumber = 1, _pageSize = 10;
    public int PageNumber { get => _pageNumber; set => _pageNumber = value < 1 ? 1 : value; }
    public int PageSize   { get => _pageSize;   set => _pageSize = value < 1 ? 10 : (value > MaxPageSize ? MaxPageSize : value); }
}

// e.g. Domain/Interfaces (next to IBookRepository) or Domain/Common
public class BookQueryOptions : PaginationParams
{
    public string? Search { get; set; }         // title/description
    public int? AuthorId { get; set; }
    public int? CategoryId { get; set; }
    public string? Language { get; set; }
    public bool? IsAvailable { get; set; }
    public string? SortBy { get; set; }          // "title" | "date" | "rating" | "downloads"
    public bool Desc { get; set; }
}

public sealed class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPrevious => PageNumber > 1;
    public bool HasNext => PageNumber < TotalPages;
}
```
Flow (EF confined to Infrastructure): the **specific repository** applies Where (filter/search) → OrderBy (sort)
→ Skip/Take (page) via the shared `ToPagedResultAsync` helper, returning `PagedResult<Entity>`. The **service**
maps entities → DTOs and returns `ApiResponse<PagedResult<TDto>>`. The WebAPI controller binds the query string
to `BookQueryOptions`.

---

## 8. Authentication Design (Custom JWT + Refresh Token)

- **Login:** verify `PasswordHash` → issue short-lived **access JWT** (claims: sub=userId, roles) + a long-lived **refresh token**.
- **Refresh token** is a new entity (see `03-Data-Model-ERD.md`): stored **hashed**, with `ExpiresAt`, `RevokedAt`, `ReplacedByToken`.
- **Refresh flow:** validate incoming refresh token → rotate (revoke old, issue new) → return new pair.
- **Logout:** revoke the active refresh token.
- **Authorization:** `[Authorize(Roles = "Admin")]` etc.; policies for ownership checks (own comment/book).

```csharp
public class JwtOptions
{
    public string Issuer { get; set; } = default!;
    public string Audience { get; set; } = default!;
    public string SecretKey { get; set; } = default!;      // from user-secrets / env, NOT source control
    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 7;
}
```

---

## 9. Physical Solution Structure

```
DigitalBookLibrary.sln
│
├── src/
│   ├── DigitalBookLibrary.Domain/
│   │   ├── Entities/            (the 13 existing entities + RefreshToken)
│   │   ├── Common/              (PagedResult<T>, PaginationParams)
│   │   ├── Errors/              (Error.cs, BookErrors.cs, UserErrors.cs, ...)
│   │   ├── Exceptions/          (AppException.cs + subclasses — HTTP-free)
│   │   └── Interfaces/          (IRepository<T>, IUnitOfWork, IBookRepository, query-option records ...)
│   │
│   ├── DigitalBookLibrary.Application/
│   │   ├── Common/              (ApiResponse, ResponseCodes)
│   │   ├── Services/            (AuthService, BookService, ... 9 concrete services — NO IService interfaces)
│   │   ├── Interfaces/          (PORT interfaces only: IFileStorageService, IJwtProvider, IPasswordHasher, ICurrentUser)
│   │   ├── DTOs/                (per module: BookDtos, AuthDtos, ...)
│   │   ├── Mapping/             (manual mapping extension methods: BookMappings.ToDto(), ...)
│   │   ├── Validation/          (FluentValidation validators)
│   │   └── DependencyInjection.cs   (AddApplication)
│   │
│   ├── DigitalBookLibrary.Infrastructure/
│   │   ├── Persistence/         (AppDbContext, UnitOfWork, Repository<T>, Configurations/)
│   │   ├── Persistence/Migrations/
│   │   ├── Identity/            (JwtProvider, PasswordHasher)
│   │   ├── Files/               (LocalFileStorageService)
│   │   ├── Auditing/            (AuditSaveChangesInterceptor)
│   │   └── DependencyInjection.cs   (AddInfrastructure)
│   │
│   └── DigitalBookLibrary.WebAPI/
│       ├── Controllers/
│       ├── Middleware/          (GlobalExceptionMiddleware)
│       ├── Extensions/          (Swagger, CORS, Auth setup)
│       ├── appsettings.json
│       └── Program.cs
│
├── tests/                       (DigitalBookLibrary.UnitTests)
└── docs/                        (this documentation)
```

### 9.1 Application DI (as requested)
```csharp
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // No AutoMapper — mapping is manual via extension methods (Mapping/*.cs).
        services.AddScoped<AuthService>();
        services.AddScoped<AuthorService>();
        services.AddScoped<CategoryService>();
        services.AddScoped<BookService>();
        services.AddScoped<RatingService>();
        services.AddScoped<SavedBookService>();
        services.AddScoped<CommentService>();
        services.AddScoped<BookActivityService>();
        services.AddScoped<DashboardService>();          // admin dashboard / statistics
        // + validators (FluentValidation) registered here
        return services;
    }
}
```
> **Decision: services have NO interfaces.** Each service has a single implementation, so an `IBookService`
> would be pure boilerplate. Controllers depend on the **concrete** service. What *must* stay as interfaces are
> the **ports** the services depend on — `IRepository<T>`, `IUnitOfWork`, `IJwtProvider`, `IPasswordHasher`,
> `IFileStorageService`, `ICurrentUser` — because they cross layer boundaries and are mocked in unit tests.
> This keeps the code lean while remaining fully testable (we test the concrete service with mocked ports).

### 9.2 Manual mapping convention (replaces AutoMapper)
Each module has a static mapping class in `Application/Mapping/`. Mapping is explicit, compile-checked, and trivial to read/debug.
```csharp
// Application/Mapping/BookMappings.cs
public static class BookMappings
{
    public static BookListDto ToListDto(this Book b) => new()
    {
        Id = b.Id, Title = b.Title,
        AuthorName = b.Author?.Person?.FullName ?? string.Empty,
        CategoryName = b.Category?.Name ?? string.Empty,
        DownloadsCount = b.DownloadsCount, ReadsCount = b.ReadsCount
    };

    public static BookDetailsDto ToDetailsDto(this Book b, double avgRating, int ratingCount) => new()
    {
        Id = b.Id, Title = b.Title, Description = b.Description,
        ImageUrl = b.ImageUrl, Pages = b.Pages, Language = b.Language,
        AverageRating = avgRating, RatingCount = ratingCount
        // ...
    };
}
```
For list endpoints, prefer projecting **directly to the DTO inside the `IQueryable`** (EF translates it to SQL,
selecting only needed columns) — even cleaner and faster than mapping in memory.

---

## 10. Cross-cutting Conventions
- **Async everywhere**, with `CancellationToken` flowing from controller → service → repository.
- **`AsNoTracking()`** for all read queries; tracking only when mutating.
- **DTO projection** in queries where possible to avoid over-fetching.
- **No business logic in controllers** — they call one service method and wrap the result.
- **Secrets** (JWT key, connection string) via `dotnet user-secrets` / environment variables, never committed.
- **Nullable reference types** enabled; treat warnings as errors in CI (optional).

---

## 11. File Upload & Download Design

Files (book PDFs, cover images) are abstracted behind `IFileStorageService` so local storage can later be
swapped for cloud (S3/Azure) without touching services.

```csharp
// Application/Interfaces/IFileStorageService.cs
public interface IFileStorageService
{
    Task<string> SaveAsync(FileUploadRequest file, string folder, CancellationToken ct = default); // returns stored path/URL
    Task<Stream> OpenReadAsync(string storedPath, CancellationToken ct = default);
    Task DeleteAsync(string storedPath, CancellationToken ct = default);
}

// Application/Common/FileUploadRequest.cs  — framework-agnostic (no IFormFile in Application!)
public sealed class FileUploadRequest
{
    public required Stream Content { get; init; }
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
    public long Length { get; init; }
}
```
> **Clean boundary:** `IFormFile` is an ASP.NET type — it must **not** leak into Application. The controller
> converts `IFormFile` → `FileUploadRequest` at the WebAPI edge; services depend only on the abstraction.

### 11.1 Validation rules (enforced before saving)
| Rule | PDF | Cover image |
|------|-----|-------------|
| Allowed extensions | `.pdf` | `.jpg` `.jpeg` `.png` `.webp` |
| Allowed content-type | `application/pdf` | `image/jpeg` `image/png` `image/webp` |
| Max size | 50 MB (configurable) | 5 MB |
| Filename | GUID-based, never trust client name | same |
| Content check | verify magic-number/header, not just extension | same |

Violations throw `ValidationAppException` with codes like `FILE_TYPE_NOT_ALLOWED`, `FILE_TOO_LARGE`.

### 11.2 Storage layout & config
```
wwwroot/uploads/books/{guid}.pdf
wwwroot/uploads/covers/{guid}.jpg
```
```csharp
public class FileStorageOptions
{
    public string RootPath { get; set; } = "wwwroot/uploads";
    public long MaxPdfBytes { get; set; } = 50 * 1024 * 1024;
    public long MaxImageBytes { get; set; } = 5 * 1024 * 1024;
}
```
### 11.3 Download / read (streaming)
- Never load a whole file into memory — stream it: `return File(stream, "application/pdf", downloadName)`.
- Downloading routes through `BookActivityService` which enforces `Book.IncrementDownloads()` (availability rule),
  writes the log, and commits via UoW **before** streaming.
- Large uploads: keep `[RequestSizeLimit]` / Kestrel limits aligned with `MaxPdfBytes`.

---

## 12. Clean Code Conventions (project-wide)

These are enforced in review; the goal is code any developer can read top-to-bottom without surprises.

**Naming & structure**
- Intention-revealing names; no abbreviations (`downloadsCount`, not `dc`). Async methods end with `Async`.
- One class per file; file name = type name. Folders mirror namespaces.
- Interfaces prefixed `I`; DTOs suffixed `Dto`; validators suffixed `Validator`.

**Methods & functions**
- Small, single-responsibility methods; a service method reads like a short paragraph.
- Guard clauses / early returns over deep nesting (max ~2 levels).
- No boolean "flag" parameters that change behavior — split into two methods.

**Design**
- Controllers are thin: validate model → call one service method → wrap in `ApiResponse`. No business logic.
- Services own business rules; repositories own data access only (no rules).
- Depend on abstractions (`IRepository<T>`, `IUnitOfWork`, `IFileStorageService`), not concretions.
- No magic numbers/strings → constants or the `Error`/config catalogs.
- DRY, but prefer a little duplication over the wrong abstraction.

**Errors & nulls**
- Fail fast with the right `AppException` + `Error` code; never swallow exceptions silently.
- Nullable reference types on; avoid returning `null` collections (return empty).

**Consistency**
- `.editorconfig` enforces formatting; `dotnet format` in CI.
- Every public endpoint returns `ApiResponse`; every list endpoint paginates.
- Comments explain **why**, not **what**; delete dead/commented-out code.

---

## 13. API Documentation — Swagger / OpenAPI

Interactive docs via **Swashbuckle** (`Swagger UI`), the primary way to explore and test the API.

- Enabled in Development/Staging; toggle for Production via config (NFR-7).
- **JWT support in the UI:** an `Authorize` button so you can paste a bearer token and call protected endpoints.
- **XML comments** on controllers/DTOs feed endpoint & field descriptions into the schema.
- Endpoints grouped by controller/tag (Auth, Books, Ratings, Dashboard, ...).
- Document the `ApiResponse<T>` envelope and common error responses with `[ProducesResponseType]`.

```csharp
// WebAPI/Extensions/SwaggerSetup.cs (sketch)
services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new() { Title = "Digital Book Library API", Version = "v1" });
    o.IncludeXmlComments(xmlPath);                     // enable <summary> docs
    o.AddSecurityDefinition("Bearer", new()            // JWT box in Swagger UI
    {
        In = ParameterLocation.Header, Name = "Authorization",
        Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT"
    });
    o.AddSecurityRequirement(/* require Bearer on secured endpoints */);
});
```
> Also ship a `requests.http` file (VS Code REST Client) / Postman collection as a lightweight alternative.
