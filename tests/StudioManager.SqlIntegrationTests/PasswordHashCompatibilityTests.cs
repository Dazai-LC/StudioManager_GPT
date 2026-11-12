using System.Security.Cryptography;
using StudioManager.Infrastructure;
using Xunit;

namespace StudioManager.SqlIntegrationTests;

// Pure cryptographic unit tests: no SQL connection or application configuration.
public sealed class PasswordHashCompatibilityTests
{
    [Fact]
    public void LegacyPbkdf2Sha256Encoding_IsStillAcceptedWithoutRewritingCredentials()
    {
        const string password = "Synthetic-Legacy-Password-4!";
        var salt = Enumerable.Range(1, 16).Select(value => (byte)value).ToArray();
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 210_000, HashAlgorithmName.SHA256, 32);
        var legacy = $"PBKDF2-SHA256$210000${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
        var hasher = new Pbkdf2PasswordHasher();
        Assert.True(hasher.Verify(password, legacy));
        Assert.False(hasher.Verify("incorrect", legacy));
    }

    [Fact]
    public void NewHashes_UseIndependentSaltsAndRoundTrip()
    {
        const string password = "Synthetic-Current-Password-8!";
        var hasher = new Pbkdf2PasswordHasher();
        var first = hasher.Hash(password);
        var second = hasher.Hash(password);
        Assert.NotEqual(first, second);
        Assert.True(hasher.Verify(password, first));
        Assert.True(hasher.Verify(password, second));
    }

    [Theory]
    [InlineData("")]
    [InlineData("unsupported$210000$a$b")]
    [InlineData("PBKDF2-SHA256$invalid$invalid$invalid")]
    [InlineData("PBKDF2-SHA256$-1$AQ==$AQ==")]
    public void MalformedHashes_FailClosed(string encoded)
        => Assert.False(new Pbkdf2PasswordHasher().Verify("password", encoded));
}
