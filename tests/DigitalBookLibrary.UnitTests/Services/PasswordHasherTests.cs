using DigitalBookLibrary.Infrastructure.Identity;
using Shouldly;
using Xunit;

namespace DigitalBookLibrary.UnitTests.Services
{
    // T-5 — the real PBKDF2 hasher round-trips: a password verifies against its own hash and
    // nothing else. This is the one test that runs production crypto rather than a mock.
    public class PasswordHasherTests
    {
        private readonly PasswordHasher _hasher = new();

        [Fact]
        public void Verify_CorrectPassword_ReturnsTrue()
        {
            var hash = _hasher.Hash("Sup3r$ecret");

            _hasher.Verify("Sup3r$ecret", hash).ShouldBeTrue();
        }

        [Fact]
        public void Verify_WrongPassword_ReturnsFalse()
        {
            var hash = _hasher.Hash("Sup3r$ecret");

            _hasher.Verify("wrong-password", hash).ShouldBeFalse();
        }

        [Fact]
        public void Hash_SamePasswordTwice_ProducesDifferentHashes()
        {
            // A per-password random salt means two hashes of the same input must differ, yet both verify.
            var first = _hasher.Hash("same-input");
            var second = _hasher.Hash("same-input");

            first.ShouldNotBe(second);
            _hasher.Verify("same-input", first).ShouldBeTrue();
            _hasher.Verify("same-input", second).ShouldBeTrue();
        }

        [Fact]
        public void Verify_MalformedHash_ReturnsFalseInsteadOfThrowing()
        {
            _hasher.Verify("whatever", "not-a-valid-hash").ShouldBeFalse();
        }
    }
}
