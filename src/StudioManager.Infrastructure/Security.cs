using System.Security.Cryptography;
using StudioManager.Application;

namespace StudioManager.Infrastructure;

public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const int Iterations = 210_000;
    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, 32);
        return $"PBKDF2-SHA256${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }
    public bool Verify(string password, string encoded)
    {
        try
        {
            var p = encoded.Split('$');
            if (p.Length != 4 || p[0] != "PBKDF2-SHA256") return false;
            var salt = Convert.FromBase64String(p[2]);
            var expected = Convert.FromBase64String(p[3]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, int.Parse(p[1]), HashAlgorithmName.SHA256, expected.Length);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch { return false; }
    }
}
