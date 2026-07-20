# Error & Message Codes Catalog
## Digital Book Library API

This is the **single source of truth** for every code the API can return.
The backend ships **codes only** (in `ApiResponse.Message` and `ApiResponse.Errors[]`); the **React frontend**
owns all translation. The technical `Description` column is for **developers/logs only** and never leaves the server.

> **How the status is decided:** the Application layer throws a semantic exception (e.g. `NotFoundException`);
> the WebAPI middleware maps the exception **type** → HTTP status. The "HTTP" column below is that effective status,
> shown here for frontend reference — the Application code contains no HTTP numbers.

Convention: `UPPER_SNAKE_CASE`, pattern `<DOMAIN>_<CONDITION>`, globally unique.

---

## 1. General / Common
| Code | HTTP | Kind | Description (logs only) |
|------|------|------|-------------------------|
| `SUCCESS` | 200 | success | Generic success. |
| `CREATED` | 201 | success | Resource created. |
| `UPDATED` | 200 | success | Resource updated. |
| `DELETED` | 200 | success | Resource deleted. |
| `OPERATION_FAILED` | * | error (Message) | Generic failure wrapper; real cause is in `Errors[]`. |
| `VALIDATION_FAILED` | 400 | error | One or more input validation rules failed; see `Errors[]`. |
| `UNAUTHORIZED` | 401 | error | Authentication required or missing/invalid token. |
| `FORBIDDEN` | 403 | error | Authenticated but not allowed (role/ownership). |
| `NOT_FOUND` | 404 | error | Generic resource not found. |
| `INTERNAL_SERVER_ERROR` | 500 | error | Unhandled server error (details in logs only). |

## 2. Auth & Account
| Code | HTTP | Description (logs only) |
|------|------|-------------------------|
| `REGISTERED` | 201 | Account created (success). |
| `LOGGED_IN` | 200 | Login success. |
| `LOGGED_OUT` | 200 | Logout success. |
| `TOKEN_REFRESHED` | 200 | Access token refreshed (success). |
| `AUTH_INVALID_CREDENTIALS` | 401 | Email/username or password incorrect. |
| `AUTH_TOKEN_INVALID` | 401 | Access token malformed or signature invalid. |
| `AUTH_TOKEN_EXPIRED` | 401 | Access token expired. |
| `AUTH_REFRESH_TOKEN_INVALID` | 401 | Refresh token not found / does not match. |
| `AUTH_REFRESH_TOKEN_EXPIRED` | 401 | Refresh token past its expiry. |
| `AUTH_REFRESH_TOKEN_REVOKED` | 401 | Refresh token already revoked/rotated. |
| `AUTH_PASSWORD_TOO_WEAK` | 400 | Password fails complexity policy. |
| `USER_NOT_FOUND` | 404 | No user with the given id/email. |
| `USER_EMAIL_IN_USE` | 409 | Email already registered. |
| `USER_USERNAME_IN_USE` | 409 | Username already taken. |
| `USER_INACTIVE` | 403 | Account is deactivated (`IsActive = false`). |
| `USER_CANNOT_MODIFY_SELF` | 400 | An admin cannot deactivate their own account or remove their own Admin role. |
| `USER_ROLE_INVALID` | 400 | One or more of the given role names do not exist. |
| `USER_LAST_ADMIN` | 400 | The last remaining active Admin cannot be demoted or deactivated. |

## 3. Book
| Code | HTTP | Description (logs only) |
|------|------|-------------------------|
| `BOOK_NOT_FOUND` | 404 | No book with the supplied id. |
| `BOOK_NOT_AVAILABLE` | 409 | Book is marked unavailable; cannot be downloaded. |
| `BOOK_NOT_VISIBLE` | 404 | Book hidden from public; treated as not found for guests. |
| `BOOK_FILE_MISSING` | 404 | Book has no PDF associated. |
| `BOOK_TITLE_REQUIRED` | 400 | Title is empty. |
| `BOOK_AUTHOR_REQUIRED` | 400 | AuthorId missing/invalid. |
| `BOOK_CATEGORY_REQUIRED` | 400 | CategoryId missing/invalid. |
| `BOOK_ACCESS_DENIED` | 403 | Non-owner, non-admin tried to modify the book. |

## 4. Author
| Code | HTTP | Description (logs only) |
|------|------|-------------------------|
| `AUTHOR_NOT_FOUND` | 404 | No author with the supplied id. |
| `AUTHOR_NAME_REQUIRED` | 400 | Linked Person full name is empty. |
| `AUTHOR_HAS_BOOKS` | 409 | Cannot delete an author that still has books. |

## 5. Category
| Code | HTTP | Description (logs only) |
|------|------|-------------------------|
| `CATEGORY_NOT_FOUND` | 404 | No category with the supplied id. |
| `CATEGORY_NAME_REQUIRED` | 400 | Name is empty. |
| `CATEGORY_HAS_BOOKS` | 409 | Cannot delete a category with books. |
| `CATEGORY_HAS_CHILDREN` | 409 | Cannot delete a category with child categories. |
| `CATEGORY_PARENT_INVALID` | 400 | Parent id invalid or would create a cycle. |

## 6. Rating
| Code | HTTP | Description (logs only) |
|------|------|-------------------------|
| `RATING_OUT_OF_RANGE` | 400 | Value not in 1–5. |
| `RATING_NOT_FOUND` | 404 | No rating by this user for this book (on delete). |

## 7. Comment
| Code | HTTP | Description (logs only) |
|------|------|-------------------------|
| `COMMENT_NOT_FOUND` | 404 | No comment with the supplied id. |
| `COMMENT_TEXT_REQUIRED` | 400 | Comment text is empty. |
| `COMMENT_ACCESS_DENIED` | 403 | Non-owner, non-admin tried to edit/delete. |
| `COMMENT_PARENT_INVALID` | 400 | Parent comment missing or belongs to another book. |

## 8. Saved Book
| Code | HTTP | Description (logs only) |
|------|------|-------------------------|
| `SAVED_BOOK_NOT_FOUND` | 404 | Book not in the user's saved list (on remove). |

## 9. File Upload
| Code | HTTP | Description (logs only) |
|------|------|-------------------------|
| `FILE_REQUIRED` | 400 | No file supplied. |
| `FILE_TYPE_NOT_ALLOWED` | 400 | Extension/content-type not permitted. |
| `FILE_TOO_LARGE` | 400 | Exceeds configured max size. |
| `FILE_CORRUPTED` | 400 | Magic-number/header check failed. |
| `FILE_SAVE_FAILED` | 500 | Storage write failed (details in logs). |

## 10. Dashboard / Admin
Dashboard endpoints reuse `UNAUTHORIZED` / `FORBIDDEN`; no dedicated codes needed for the MVP.

## 10b. Field validation (FluentValidation) — all HTTP 400
Emitted in `ApiResponse.Errors[]` (with `Message = VALIDATION_FAILED`) when input DTOs fail validation.
| Code | Field | Rule |
|------|-------|------|
| `USERNAME_REQUIRED` | Username | not empty |
| `USERNAME_TOO_SHORT` | Username | min length 3 |
| `USERNAME_TOO_LONG` | Username | max length 100 |
| `EMAIL_REQUIRED` | Email | not empty |
| `EMAIL_INVALID` | Email | valid email format |
| `EMAIL_TOO_LONG` | Email | max length 256 |
| `PASSWORD_REQUIRED` | Password | not empty |
| `PASSWORD_TOO_SHORT` | Password | min length 6 |
| `PASSWORD_TOO_LONG` | Password | max length 100 |
| `IDENTIFIER_REQUIRED` | Identifier (login) | not empty |
| `BOOK_TITLE_TOO_LONG` | Book.Title | max length 300 |
| `BOOK_DESCRIPTION_TOO_LONG` | Book.Description | max length 4000 |
| `BOOK_LANGUAGE_TOO_LONG` | Book.Language | max length 50 |
| `BOOK_PAGES_INVALID` | Book.Pages | must be > 0 |
| `AUTHOR_NAME_TOO_LONG` | Author.FullName | max length 200 |
| `AUTHOR_BIO_TOO_LONG` | Author.Bio | max length 2000 |
| `AUTHOR_NATIONALITY_TOO_LONG` | Author.Nationality | max length 100 |
| `CATEGORY_NAME_TOO_LONG` | Category.Name | max length 150 |
| `ROLE_NOT_FOUND` | — (500) | seeded 'Member' role missing (server misconfig) |

> `BOOK_TITLE_REQUIRED`, `BOOK_AUTHOR_REQUIRED`, `BOOK_CATEGORY_REQUIRED`, `AUTHOR_NAME_REQUIRED`,
> `CATEGORY_NAME_REQUIRED` and `CATEGORY_PARENT_INVALID` are reused from their entity sections above.
> More field-validation codes are added here as each feature's validators are written.

> **Framework auth failures:** `[Authorize]` short-circuits before the exception middleware, so JwtBearer
> `OnChallenge`/`OnForbidden` events emit the same envelope with `UNAUTHORIZED` (401) / `FORBIDDEN` (403).

---

## 11. Frontend integration (React)

Keep a single dictionary keyed by these codes. The backend never sends display text — you translate here.

```ts
// src/i18n/apiCodes.ts
export const apiMessages: Record<string, { en: string; ar: string }> = {
  // general
  SUCCESS:               { en: "Done successfully.",            ar: "تمت العملية بنجاح." },
  CREATED:               { en: "Created successfully.",         ar: "تم الإنشاء بنجاح." },
  UPDATED:               { en: "Updated successfully.",         ar: "تم التحديث بنجاح." },
  DELETED:               { en: "Deleted successfully.",         ar: "تم الحذف بنجاح." },
  OPERATION_FAILED:      { en: "Operation failed.",             ar: "فشلت العملية." },
  VALIDATION_FAILED:     { en: "Please check your input.",      ar: "يرجى التحقق من المدخلات." },
  UNAUTHORIZED:          { en: "Please sign in.",               ar: "يرجى تسجيل الدخول." },
  FORBIDDEN:             { en: "You don't have permission.",    ar: "لا تملك الصلاحية." },
  INTERNAL_SERVER_ERROR: { en: "Something went wrong.",         ar: "حدث خطأ ما." },
  // auth
  AUTH_INVALID_CREDENTIALS: { en: "Invalid email or password.", ar: "البريد أو كلمة المرور غير صحيحة." },
  USER_EMAIL_IN_USE:        { en: "Email already in use.",      ar: "البريد مستخدم مسبقاً." },
  USER_INACTIVE:            { en: "Account is deactivated.",    ar: "الحساب معطّل." },
  USER_CANNOT_MODIFY_SELF:  { en: "You cannot deactivate or de-admin your own account.",
                              ar: "لا يمكنك تعطيل حسابك أو إزالة صلاحية الأدمن عن نفسك." },
  USER_ROLE_INVALID:        { en: "Unknown role name.",         ar: "اسم دور غير معروف." },
  USER_LAST_ADMIN:          { en: "The last admin cannot be removed.",
                              ar: "لا يمكن إزالة آخر مسؤول." },
  // book
  BOOK_NOT_FOUND:        { en: "Book not found.",               ar: "الكتاب غير موجود." },
  BOOK_NOT_AVAILABLE:    { en: "Book is not available.",        ar: "الكتاب غير متاح حالياً." },
  // rating / comment / file ... (add every code from this catalog)
};

export const translate = (code: string, lang: "en" | "ar" = "ar") =>
  apiMessages[code]?.[lang] ?? code;

// usage after an API call:
// const res = await api.get(...); // { success, message, data, errors }
// if (!res.success) toast.error(translate(res.errors[0] ?? res.message));
```

> **Rule for the frontend team:** every code in this catalog must have an entry in `apiMessages`.
> When the backend adds a code here, add its translation there — that is the only place display text lives.
