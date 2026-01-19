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
    /// Generates a random join code for events.
    /// </summary>
    string GenerateJoinCode();
}
