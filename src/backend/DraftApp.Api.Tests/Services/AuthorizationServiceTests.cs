using DraftApp.Api.Services;

namespace DraftApp.Api.Tests.Services;

/// <summary>
/// Tests for PIN hashing, token generation and token hashing.
/// </summary>
public class AuthorizationServiceTests
{
    private readonly AuthorizationService service = new();

    [Fact]
    public void VerifyPin_AcceptsCorrectPin_RejectsWrongPin()
    {
        var hash = service.HashPin("2468");

        Assert.True(service.VerifyPin("2468", hash));
        Assert.False(service.VerifyPin("2469", hash));
    }

    [Fact]
    public void HashPin_UsesRandomSalt()
    {
        Assert.NotEqual(service.HashPin("2468"), service.HashPin("2468"));
    }

    [Fact]
    public void VerifyPin_WithMalformedHash_ReturnsFalse()
    {
        Assert.False(service.VerifyPin("2468", "not-base64!"));
    }

    [Fact]
    public void HashToken_IsDeterministicLowercaseSha256Hex()
    {
        var token = service.GenerateToken();

        var hash = service.HashToken(token);

        Assert.Equal(hash, service.HashToken(token));
        Assert.Equal(64, hash.Length);
        Assert.Matches("^[0-9a-f]{64}$", hash);
        Assert.NotEqual(hash, service.HashToken(service.GenerateToken()));
    }

    [Fact]
    public void HashToken_MatchesKnownVector()
    {
        // SHA-256("abc"), the FIPS 180-2 test vector; the SQL migration must produce the same value
        Assert.Equal("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad", service.HashToken("abc"));
    }

    [Fact]
    public void GenerateToken_IsUrlSafeAnd256Bits()
    {
        var token = service.GenerateToken();

        Assert.Equal(43, token.Length); // 32 bytes, base64url without padding
        Assert.Matches("^[A-Za-z0-9_-]+$", token);
    }
}
