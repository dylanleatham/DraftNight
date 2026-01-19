using System.Security.Cryptography;

namespace DraftApp.Api.Services;

/// <summary>
/// Implementation of authorization service using secure hashing and token generation.
/// </summary>
public class AuthorizationService : IAuthorizationService
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 100000;
    private static readonly HashAlgorithmName HashAlgorithm = HashAlgorithmName.SHA256;

    public string HashPin(string pin)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(pin, salt, Iterations, HashAlgorithm, HashSize);

        var result = new byte[SaltSize + HashSize];
        Buffer.BlockCopy(salt, 0, result, 0, SaltSize);
        Buffer.BlockCopy(hash, 0, result, SaltSize, HashSize);

        return Convert.ToBase64String(result);
    }

    public bool VerifyPin(string pin, string storedHash)
    {
        try
        {
            var hashBytes = Convert.FromBase64String(storedHash);
            if (hashBytes.Length != SaltSize + HashSize)
            {
                return false;
            }

            var salt = new byte[SaltSize];
            Buffer.BlockCopy(hashBytes, 0, salt, 0, SaltSize);

            var storedHashPart = new byte[HashSize];
            Buffer.BlockCopy(hashBytes, SaltSize, storedHashPart, 0, HashSize);

            var computedHash = Rfc2898DeriveBytes.Pbkdf2(pin, salt, Iterations, HashAlgorithm, HashSize);

            return CryptographicOperations.FixedTimeEquals(storedHashPart, computedHash);
        }
        catch
        {
            return false;
        }
    }

    public string GenerateToken()
    {
        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(tokenBytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }

    public string GenerateJoinCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var codeChars = new char[6];
        var randomBytes = RandomNumberGenerator.GetBytes(6);

        for (int i = 0; i < 6; i++)
        {
            codeChars[i] = chars[randomBytes[i] % chars.Length];
        }

        return new string(codeChars);
    }
}
