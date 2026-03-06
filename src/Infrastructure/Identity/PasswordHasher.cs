using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace LotusDharma.Infrastructure.Identity;

public interface IPasswordHasher
{
    string HashPassword(string password);
    bool VerifyPassword(string hashedPassword, string providedPassword);
}

public class PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 128 / 8;
    private const int HashSize = 256 / 8;
    private const int Iterations = 600_000; // OWASP 2023 recommendation for PBKDF2-HMACSHA256
    private const byte FormatVersion = 0x01;

    public string HashPassword(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);

        byte[] hash = KeyDerivation.Pbkdf2(
            password: password,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA512,
            iterationCount: Iterations,
            numBytesRequested: HashSize);

        // Format: [version(1)][salt(16)][hash(32)]
        byte[] hashBytes = new byte[1 + SaltSize + HashSize];
        hashBytes[0] = FormatVersion;
        Array.Copy(salt, 0, hashBytes, 1, SaltSize);
        Array.Copy(hash, 0, hashBytes, 1 + SaltSize, HashSize);

        return Convert.ToBase64String(hashBytes);
    }

    public bool VerifyPassword(string hashedPassword, string providedPassword)
    {
        try
        {
            byte[] hashBytes = Convert.FromBase64String(hashedPassword);

            if (hashBytes.Length == SaltSize + HashSize)
                return VerifyLegacyPassword(hashBytes, providedPassword);

            if (hashBytes.Length != 1 + SaltSize + HashSize || hashBytes[0] != FormatVersion)
                return false;

            byte[] salt = new byte[SaltSize];
            Array.Copy(hashBytes, 1, salt, 0, SaltSize);

            byte[] expectedHash = new byte[HashSize];
            Array.Copy(hashBytes, 1 + SaltSize, expectedHash, 0, HashSize);

            byte[] actualHash = KeyDerivation.Pbkdf2(
                password: providedPassword,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA512,
                iterationCount: Iterations,
                numBytesRequested: HashSize);

            return CryptographicOperations.FixedTimeEquals(expectedHash, actualHash);
        }
        catch
        {
            return false;
        }
    }

    // Supports verifying passwords hashed with the old format (10,000 iterations, HMACSHA256)
    private static bool VerifyLegacyPassword(byte[] hashBytes, string providedPassword)
    {
        byte[] salt = new byte[SaltSize];
        Array.Copy(hashBytes, 0, salt, 0, SaltSize);

        byte[] expectedHash = new byte[HashSize];
        Array.Copy(hashBytes, SaltSize, expectedHash, 0, HashSize);

        byte[] actualHash = KeyDerivation.Pbkdf2(
            password: providedPassword,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: 10000,
            numBytesRequested: HashSize);

        return CryptographicOperations.FixedTimeEquals(expectedHash, actualHash);
    }
}
