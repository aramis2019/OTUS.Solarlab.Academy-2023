using System.Security.Cryptography;

namespace Board.Application.AppData.Contexts.Accounts.Services;

/// <summary>
/// Хеширование паролей по PBKDF2 (HMAC-SHA256) со случайной солью.
/// Формат хеша: <c>{итерации}.{соль base64}.{хеш base64}</c>.
/// </summary>
public class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 600_000;
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

    /// <inheritdoc />
    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algorithm, HashSize);
        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    /// <inheritdoc />
    public bool Verify(string password, string passwordHash)
    {
        if (!TryParse(passwordHash, out var iterations, out var salt, out var expected))
        {
            return false;
        }

        var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, Algorithm, expected.Length);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    /// <summary>
    /// Проверить, что строка имеет формат хеша этого алгоритма (а не, например, пароль в открытом виде).
    /// </summary>
    public static bool IsHash(string value) => TryParse(value, out _, out _, out _);

    private static bool TryParse(string passwordHash, out int iterations, out byte[] salt, out byte[] hash)
    {
        salt = hash = Array.Empty<byte>();
        var parts = passwordHash.Split('.');
        if (parts.Length != 3 || !int.TryParse(parts[0], out iterations) || iterations <= 0)
        {
            iterations = 0;
            return false;
        }

        try
        {
            salt = Convert.FromBase64String(parts[1]);
            hash = Convert.FromBase64String(parts[2]);
        }
        catch (FormatException)
        {
            return false;
        }

        return salt.Length > 0 && hash.Length > 0;
    }
}
