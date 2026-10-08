using Board.Application.AppData.Contexts.Accounts.Services;
using Shouldly;
using Xunit;

namespace Board.Tests
{
    public class Pbkdf2PasswordHasherTests
    {
        private readonly Pbkdf2PasswordHasher _hasher = new();

        [Fact]
        public void Hash_ThenVerify_Succeeds()
        {
            var hash = _hasher.Hash("secret-password");

            hash.ShouldNotContain("secret-password");
            _hasher.Verify("secret-password", hash).ShouldBeTrue();
            _hasher.Verify("wrong-password", hash).ShouldBeFalse();
        }

        [Fact]
        public void Hash_UsesRandomSalt()
        {
            _hasher.Hash("same").ShouldNotBe(_hasher.Hash("same"));
        }

        [Theory]
        [InlineData("plain-text-password")]
        [InlineData("1.notbase64.notbase64")]
        [InlineData("")]
        public void Verify_InvalidHashFormat_ReturnsFalse(string storedHash)
        {
            _hasher.Verify("plain-text-password", storedHash).ShouldBeFalse();
        }
    }
}
