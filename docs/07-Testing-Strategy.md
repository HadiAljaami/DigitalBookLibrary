# Testing Strategy
## Digital Book Library API

Testing is **in scope** for this project — a focused suite that proves the critical business rules.
Because services have **no interfaces**, we test the **concrete service** directly and **mock its ports**
(`IRepository<T>`, `IUnitOfWork`, `IJwtProvider`, ...). No-interface services remain 100% unit-testable.

---

## 1. Tooling  *(as built)*
| Tool | Purpose |
|------|---------|
| **xUnit** 2.9 | Test framework/runner. |
| **NSubstitute** 5.3 | Mocks the port interfaces the services depend on. |
| **Shouldly** 4.3 | Readable assertions (`result.ShouldBe(...)`). |
| **Microsoft.NET.Test.Sdk** + **coverlet.collector** | Test host and coverage. |
| **WebApplicationFactory** *(optional, later)* | End-to-end API tests over the real pipeline. Not built yet. |

Test project: `tests/DigitalBookLibrary.UnitTests` references Application + Domain + Infrastructure
(the last only for T-5, which exercises the real `PasswordHasher`).

## 2. What we test (priority = business rules)
| # | Rule under test | Layer | Notes |
|---|-----------------|-------|-------|
| T-1 | Rating is **1–5** only → `RATING_OUT_OF_RANGE` otherwise. | RatingService | boundary values 0,1,5,6. |
| T-2 | A user rating the same book twice **updates** (no duplicate row). | RatingService | one-per-user rule (FR-RATE-2). |
| T-3 | Downloading an **unavailable** book throws (`Book.IncrementDownloads`). | Domain/BookActivityService | domain invariant. |
| T-4 | Download increments counter **and** writes a log in one save. | BookActivityService | verify UoW.SaveChanges called once; log added. |
| T-5 | Password hash **verifies** correctly and rejects wrong password. | PasswordHasher | round-trip. |
| T-6 | Refresh-token **rotation**: old token revoked, new issued; revoked token rejected. | AuthService | security-critical. |
| T-7 | Login with wrong credentials / inactive user → correct error codes. | AuthService | `AUTH_INVALID_CREDENTIALS`, `USER_INACTIVE`. |
| T-8 | Category delete guarded when it has books/children. | CategoryService | `CATEGORY_HAS_BOOKS` / `_HAS_CHILDREN`. |
| T-9 | Save book is **idempotent** (no duplicate saved rows). | SavedBookService | FR-SAVE-4. |
| T-10 | Comment edit/delete by non-owner (non-admin) is forbidden. | CommentService | `COMMENT_ACCESS_DENIED`. |

## 3. Conventions
- **AAA** pattern: Arrange → Act → Assert, with blank lines separating them.
- Test name: `Method_Scenario_ExpectedResult` — e.g. `RateAsync_ValueAboveFive_ThrowsValidation`.
- One logical assertion per test (Shouldly can chain related checks).
- No real DB/network in unit tests — mock ports. Repository tests (if any) use EF InMemory/SQLite.
- Assert on **error codes**, never on message text.

## 4. Example (sketch)
```csharp
public class RatingServiceTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public async Task RateAsync_ValueOutOfRange_ThrowsValidation(int value)
    {
        // Arrange
        var repo = new Mock<IRepository<Rating>>();
        var uow  = new Mock<IUnitOfWork>();
        var sut  = new RatingService(repo.Object, /* book repo */ null!, uow.Object);

        // Act
        var act = () => sut.RateAsync(bookId: 1, userId: 1, value: value, CancellationToken.None);

        // Assert
        (await act.Should().ThrowAsync<ValidationAppException>())
            .Which.Errors.Should().ContainSingle(e => e.Code == "RATING_OUT_OF_RANGE");
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
```

## 5. Scope for the MVP
- **Done:** T-1…T-10 implemented as **25 tests** (the ten rules plus boundary/positive companions), all green.
  `dotnet test` runs offline in ~1s. CI wired via `.github/workflows/ci.yml` (restore → build → test on push/PR to main).
- **Later (optional):** integration tests with `WebApplicationFactory` hitting real endpoints through the pipeline
  (auth flow, book download), and repository tests against SQLite.

## 6. Coverage map  *(as built)*
| # | Test class · methods | Notes |
|---|----------------------|-------|
| T-1 | `RatingServiceTests` — out-of-range 0/6/-3 throw; boundaries 1/5 accepted | value guard + no save |
| T-2 | `RatingServiceTests` — re-rate updates, no second insert | one-per-user |
| T-3 | `BookActivityServiceTests` — unavailable book download throws, no count/log | domain invariant |
| T-4 | `BookActivityServiceTests` — available download increments **and** logs in one save | UoW committed once |
| T-5 | `PasswordHasherTests` — real PBKDF2 round-trip; random salt; malformed hash → false | production crypto |
| T-6 | `AuthServiceTests` — refresh rotates (old revoked+linked, new issued); revoked token rejected | security-critical |
| T-7 | `AuthServiceTests` — unknown id / wrong password → `AUTH_INVALID_CREDENTIALS`; inactive → `USER_INACTIVE` | codes, not messages |
| T-8 | `CategoryServiceTests` — delete blocked with books / children; empty leaf deletes | `CATEGORY_HAS_BOOKS`/`_HAS_CHILDREN` |
| T-9 | `SavedBookServiceTests` — save is idempotent; first save inserts once | no duplicate rows |
| T-10 | `CommentServiceTests` — non-owner non-admin edit/delete forbidden; admin moderates any | `COMMENT_ACCESS_DENIED` |
