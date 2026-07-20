# Use Case Diagram
## Digital Book Library API

Actors and the use cases they can perform. Mermaid `flowchart` is used to approximate a UML use-case view
(GitHub does not render UML use-case natively).

---

## 1. Actors & Goals

| Actor | Primary goals |
|-------|---------------|
| **Guest** | Discover content: browse/search books, authors, categories; view details & ratings. |
| **Member** | Everything a Guest can, plus: read/download, comment, rate, save books, manage profile. |
| **Publisher** | A Member who additionally manages **their own** books. |
| **Admin** | Full content & user management + moderation + audit. |

> Member inherits Guest capabilities; Publisher inherits Member; Admin inherits all.

---

## 2. Use Case Diagram (Mermaid)

```mermaid
flowchart LR
    Guest([Guest])
    Member([Member])
    Publisher([Publisher])
    Admin([Admin])

    subgraph Catalog
        UC1[Browse & search books]
        UC2[View book details]
        UC3[Browse authors]
        UC4[Browse categories tree]
    end

    subgraph Account
        UC5[Register]
        UC6[Login]
        UC7[Refresh token]
        UC8[Logout]
        UC9[View own profile]
    end

    subgraph Interaction
        UC10[Read book online]
        UC11[Download book]
        UC12[Rate a book 1-5]
        UC13[Comment / reply]
        UC14[Save / unsave book]
        UC15[View saved list]
    end

    subgraph Management
        UC16[CRUD own books]
        UC17[Upload book file / cover]
        UC18[CRUD any book]
        UC19[CRUD authors]
        UC20[CRUD categories]
        UC21[Moderate comments]
        UC22[Manage users / roles]
        UC23[Toggle visibility / availability]
        UC24[View audit log]
    end

    Guest --> UC1 & UC2 & UC3 & UC4 & UC5 & UC6

    Member --> UC7 & UC8 & UC9
    Member --> UC10 & UC11 & UC12 & UC13 & UC14 & UC15

    Publisher --> UC16 & UC17

    Admin --> UC18 & UC19 & UC20 & UC21 & UC22 & UC23 & UC24
```

---

## 3. Representative Use Case: "Member rates a book"

| Field | Value |
|-------|-------|
| **Actor** | Member |
| **Precondition** | Authenticated; book exists and is visible. |
| **Main flow** | 1. Member submits rating value (1–5) for a book. 2. System validates range. 3. If the user already rated → update; else create. 4. Persist via UoW. 5. Return updated average. |
| **Alternate** | Value outside 1–5 → `RATING_OUT_OF_RANGE` (400). Book not found → `BOOK_NOT_FOUND` (404). |
| **Postcondition** | Exactly one rating row for `(user, book)`; average reflects it. |
| **Maps to** | FR-RATE-1, FR-RATE-2, FR-RATE-3. |

## 4. Representative Use Case: "Member downloads a book"

| Field | Value |
|-------|-------|
| **Actor** | Member |
| **Precondition** | Authenticated; book exists, is available, has a PDF file. |
| **Main flow** | 1. Request download. 2. System checks availability (`Book.IncrementDownloads` enforces it). 3. Increment `DownloadsCount` + write `BookDownloadLog` in one UoW transaction. 4. Stream the file. |
| **Alternate** | Not available → `BOOK_NOT_AVAILABLE` (409). No file → `BOOK_FILE_MISSING` (404). |
| **Postcondition** | Download counter +1; one log row added. |
| **Maps to** | FR-ACT-2, FR-ACT-3, FR-ACT-4. |
