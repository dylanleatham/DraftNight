namespace DraftApp.Api.Services;

/// <summary>
/// Service for handling PIN hashing and token generation/validation.
/// </summary>
public interface IAuthorizationService
{
    /// <summary>
    /// Hashes a PIN using a secure algorithm.
    /// </summary>
    string HashPin(string pin);

    /// <summary>
    /// Verifies a PIN against a stored hash.
    /// </summary>
    bool VerifyPin(string pin, string hash);

    /// <summary>
    /// Generates a secure random token.
    /// </summary>
    string GenerateToken();

    /// <summary>
    /// Hashes a bearer token for storage. Tokens are high-entropy random values, so a fast
    /// unsalted SHA-256 is sufficient; it keeps lookups indexable while ensuring a database
    /// leak does not expose usable credentials.
    /// </summary>
    string HashToken(string token);

    /// <summary>
    /// Generates a random join code for events.
    /// </summary>
    string GenerateJoinCode();
}
