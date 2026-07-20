# Software Requirements Specification (SRS)
## Digital Book Library API

| | |
|---|---|
| **Project** | Digital Book Library (`DigitalBookLibrary`) |
| **Type** | ASP.NET Core Web API (RESTful) |
| **Version** | 1.0 (MVP) |
| **Date** | 2026-07-14 |

---

## 1. Introduction

### 1.1 Purpose
The Digital Book Library is a backend API that lets users **browse, read, download and interact with books**.
It provides authentication, role-based authorization, content management for administrators, and social
features (comments, ratings, saved lists). This SRS defines the scope, actors, and requirements that drive
the design and implementation.

### 1.2 Scope
The system exposes a REST API consumed by a future frontend (web/mobile). It covers:
- Catalog browsing (books, authors, categories) with pagination, filtering, sorting, searching.
- Reading and downloading books (with activity logging and counters).
- User accounts, authentication (JWT + refresh token), and role-based access.
- Social interaction: comments (threaded), ratings (1–5), saved/favorite books.
- Administrative CRUD for books, authors, categories.
- Auditing of sensitive operations.

Out of scope for the MVP: payments, e-commerce, notifications, recommendation engine, online PDF reader UI
(the API only serves the file/stream).

### 1.3 Definitions
| Term | Meaning |
|------|---------|
| **DTO** | Data Transfer Object — shape of data crossing the API boundary. |
| **UoW** | Unit of Work — coordinates saving changes across repositories in one transaction. |
| **ApiResponse<T>** | The single envelope wrapping every API response. |
| **Code** | A stable string key (e.g. `BOOK_NOT_FOUND`, `SUCCESS`) returned to the client for i18n. **All** client-facing text is a code, never a human sentence. |
| **MVP** | Minimum Viable Product. |

---

## 2. Actors

| Actor | Description |
|-------|-------------|
| **Guest** | Unauthenticated visitor. Can browse the public catalog and search. |
| **Member (User)** | Registered & authenticated. Can read/download, comment, rate, save books, manage own profile. |
| **Publisher** | A member whose linked `Person` is also an `Author`. Owns books via `Book.PublisherId` and manages **only their own** books. |
| **Admin** | Full management: books, authors, categories, users, comments, audit, and the **dashboard** (KPIs, charts, user management). |

> Roles are stored in `Role` and linked via `UserRole` (many-to-many). Seeded roles: `Admin`, `Member`.
>
> **Author vs. account (important):** an `Author` is backed by a `Person`, and a `Person` **may or may not** have a `UserAccount`:
> - *Classic / historical authors* → `Person` **without** a `UserAccount`; they exist as catalog data only and cannot log in.
> - *Living authors with an account* → `Person` **with** a `UserAccount`; they log in and act as **Publisher**, uploading and managing their own books.
>
> So `Book.AuthorId` = literary attribution (who wrote it), while `Book.PublisherId` (optional) = the account that uploaded/manages it.
> "Publisher" is therefore **ownership-based**, not a separate stored role. See §6, OP-2 (resolved).

---

## 3. Functional Requirements

Requirements are grouped by module. Each has an ID `FR-<module>-<n>`.

### 3.1 Authentication & Accounts (`Auth`)
| ID | Requirement | Actor |
|----|-------------|-------|
| FR-AUTH-1 | Register a new account (username, email, password) with hashed password. | Guest |
| FR-AUTH-2 | Log in with email/username + password; receive **access token (JWT)** + **refresh token**. | Guest |
| FR-AUTH-3 | Refresh an expired access token using a valid, non-revoked refresh token. | Member |
| FR-AUTH-4 | Log out → revoke the current refresh token. | Member |
| FR-AUTH-5 | Get the current authenticated user's profile (`/me`). | Member |
| FR-AUTH-6 | Passwords are never stored or returned in plaintext; only `PasswordHash` persists. | System |
| FR-AUTH-7 | Deactivated accounts (`IsActive = false`) cannot log in. | System |

### 3.2 Books (`Book`)
| ID | Requirement | Actor |
|----|-------------|-------|
| FR-BOOK-1 | List books with **pagination, filtering (author/category/language/availability), sorting, and search** (title/description). | Guest |
| FR-BOOK-2 | Get a single book's details including author, category, average rating and counts. | Guest |
| FR-BOOK-3 | Only **visible & available** books appear to guests; admins see all. | System |
| FR-BOOK-4 | Create / update / delete a book (metadata + file references). | Admin / Publisher (own) |
| FR-BOOK-5 | Upload a book's PDF file and cover image. | Admin / Publisher (own) |
| FR-BOOK-6 | Toggle a book's visibility and availability. | Admin |

### 3.3 Book Activity & Personal Library (`BookActivity`)
| ID | Requirement | Actor |
|----|-------------|-------|
| FR-ACT-1 | Read a book online → stream/serve file, log `BookReadLog`, increment `ReadsCount`. | Member |
| FR-ACT-2 | Download a book → serve file, log `BookDownloadLog`, increment `DownloadsCount`. | Member |
| FR-ACT-3 | Downloading is blocked when the book is **not available** (domain rule enforced in entity). | System |
| FR-ACT-4 | Reads/downloads counters are updated atomically with the log entry (one UoW transaction). | System |
| FR-ACT-5 | View own **read history** — the list of books the member has read, most-recent first, paginated. | Member |
| FR-ACT-6 | View own **download history** — the list of books the member has downloaded, paginated. | Member |

> Together with **Saved Books** (§3.7), FR-ACT-5/6 form the member's **"My Library"**: three tabs —
> *Read*, *Downloaded*, *Saved*. A book read/downloaded multiple times shows once (latest date) in the history view.

### 3.4 Authors (`Author`) & Categories (`Category`)
| ID | Requirement | Actor |
|----|-------------|-------|
| FR-AUTHOR-1 | List/search authors (with the linked `Person` info) — paginated. | Guest |
| FR-AUTHOR-2 | Get an author's details and their books. | Guest |
| FR-AUTHOR-3 | Create / update / delete an author. | Admin |
| FR-AUTHOR-4 | An author may exist **without** a user account (classic authors); `UserAccount` is optional on the author's `Person`. | System |
| FR-AUTHOR-5 | An author whose `Person` has a `UserAccount` acts as **Publisher** and may upload/manage their own books. | Publisher |
| FR-CAT-1 | List categories as a **tree** (parent/children). | Guest |
| FR-CAT-2 | Create / update / delete a category (with parent). | Admin |
| FR-CAT-3 | Prevent deleting a category that has books or children (or reassign). | System |

### 3.5 Comments (`Comment`)
| ID | Requirement | Actor |
|----|-------------|-------|
| FR-COM-1 | List comments of a book as a **thread** (replies nested via `ParentCommentId`). | Guest |
| FR-COM-2 | Add a comment or a reply to a book. | Member |
| FR-COM-3 | Edit / delete own comment. | Member (own) |
| FR-COM-4 | Delete any comment (moderation). | Admin |

### 3.6 Ratings (`Rating`)
| ID | Requirement | Actor |
|----|-------------|-------|
| FR-RATE-1 | Rate a book with a value **1–5**. | Member |
| FR-RATE-2 | A user may rate a given book **only once**; re-rating updates the existing value. | System |
| FR-RATE-3 | Expose a book's **average rating** and rating count. | Guest |

### 3.7 Saved Books (`SavedBook`)
| ID | Requirement | Actor |
|----|-------------|-------|
| FR-SAVE-1 | Save a book to the personal list (favorites/wishlist). | Member |
| FR-SAVE-2 | Remove a book from the saved list. | Member |
| FR-SAVE-3 | List own saved books — paginated. | Member |
| FR-SAVE-4 | Saving the same book twice is idempotent (no duplicates). | System |

### 3.8 Auditing (`AuditLog`) — cross-cutting
| ID | Requirement | Actor |
|----|-------------|-------|
| FR-AUD-1 | Record sensitive mutations (create/update/delete on Book, Author, Category, User) with old/new values, user id, IP, timestamp. | System |

### 3.9 Admin Dashboard (`Dashboard`)
Data the admin panel needs. All endpoints are **Admin-only** and read-only.
| ID | Requirement | Actor |
|----|-------------|-------|
| FR-DASH-1 | Summary KPIs: total books, total users, total authors, total categories, total downloads, total reads. | Admin |
| FR-DASH-2 | Top books by **downloads**, by **reads**, and by **average rating** (top N). | Admin |
| FR-DASH-3 | Recent activity: latest registered users, latest added books, latest comments. | Admin |
| FR-DASH-4 | Time-series: downloads & reads per day/month for a date range (for charts). | Admin |
| FR-DASH-5 | Books per category and books per language (distribution for charts). | Admin |
| FR-DASH-6 | Paged user management list (search, active/inactive filter) with roles. | Admin |
| FR-DASH-7 | Recent audit-log entries (paged, filterable by entity/action). | Admin |

> These power the admin dashboard UI (cards + charts + tables). Implemented in a dedicated `DashboardService`
> (added to the original 8 services). Heavy queries use projection + `AsNoTracking`; consider caching KPIs later.

---

## 4. Non-Functional Requirements (NFR)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-1 | **Security** | Passwords hashed (PBKDF2/BCrypt). JWT signed (HMAC-SHA256). Refresh tokens stored hashed & revocable. Role-based authorization on protected endpoints. |
| NFR-2 | **Error handling** | Every error passes through a **Global Exception Middleware** and returns a uniform `ApiResponse`. The client receives **codes only** — both `Message` and `Errors[]` carry stable codes, never human sentences. Human-readable technical descriptions (`Error.Description`) are written to the **logger only** and never leave the server. |
| NFR-3 | **Consistency** | 100% of endpoints return `ApiResponse<T>`. |
| NFR-4 | **Performance** | List endpoints must paginate (no unbounded queries). Queries use `AsNoTracking` for reads and projection to DTOs where possible. |
| NFR-5 | **Maintainability** | Clean Architecture layering; dependencies point inward; no EF Core types leak into Domain. |
| NFR-6 | **Testability** | Business logic in Services depends on interfaces (`IUnitOfWork`, `IRepository<T>`), enabling unit tests. |
| NFR-7 | **Documentation** | Swagger/OpenAPI with JWT auth support enabled in all environments except production (configurable). |
| NFR-8 | **Observability** | Structured logging (Serilog) including a correlation/trace id per request. |
| NFR-9 | **Validation** | All inbound DTOs validated (FluentValidation or DataAnnotations) before hitting Services. |
| NFR-10 | **Localization-ready** | Error codes are stable string keys so the frontend can translate them. |

---

## 5. Constraints & Assumptions
- Backend only; no server-rendered UI (pure API).
- Single database (SQL Server) accessed exclusively through EF Core in the Infrastructure layer.
- The Domain layer references **no** external framework (pure C#).
- File storage is local for the MVP, hidden behind `IFileStorageService` for future cloud swap.
- `Book ↔ Author` is **many-to-one** (a book has exactly one author), following the provided entities.

---

## 6. Open Items / To Confirm
| ID | Item | Default taken |
|----|------|---------------|
| OP-1 | DB provider | SQL Server |
| OP-2 | Is "Publisher" a role or derived from `Book.PublisherId`? | **Resolved:** ownership-based (not a stored role). A book's owner = the account in `Book.PublisherId`; admins do global CRUD. Authors may exist without any account. |
| OP-3 | Online reading = stream PDF vs. return signed URL | Serve/stream file via API for MVP. |
| OP-4 | Soft-delete vs hard-delete for books | Soft-delete via `IsVisible`/`IsAvailable`; hard-delete admin-only. |

---

## 7. Traceability
Every functional requirement maps to: a **Service method** (Application), one or more **endpoints** (see `05-API-Contract.md`),
and, where relevant, an **entity/domain rule** (see `03-Data-Model-ERD.md`).
