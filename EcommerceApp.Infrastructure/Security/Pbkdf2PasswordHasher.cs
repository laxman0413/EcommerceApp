using System.Security.Cryptography;
using EcommerceApp.Application.Common.Security;

namespace EcommerceApp.Infrastructure.Security;

// Self-contained PBKDF2-HMAC-SHA256 hasher (the same algorithm family ASP.NET Core Identity
// uses internally) so hashing has no external dependency beyond the BCL.
//
// Stored format: {iterations}.{saltBase64}.{subkeyBase64}
public class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;       // 128-bit salt
    private const int SubkeySize = 32;     // 256-bit derived key
    private const int Iterations = 210_000; // OWASP-recommended minimum for PBKDF2-SHA256 (2024+)

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var subkey = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, SubkeySize);

        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(subkey)}";
    }

    public bool Verify(string password, string hash)
    {
        var parts = hash.Split('.', 3);
        if (parts.Length != 3 || !int.TryParse(parts[0], out var iterations))
            return false;

        var salt = Convert.FromBase64String(parts[1]);
        var expectedSubkey = Convert.FromBase64String(parts[2]);

        var actualSubkey = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expectedSubkey.Length);

        // Constant-time comparison to avoid leaking timing information about the hash.
        return CryptographicOperations.FixedTimeEquals(actualSubkey, expectedSubkey);
    }
}
