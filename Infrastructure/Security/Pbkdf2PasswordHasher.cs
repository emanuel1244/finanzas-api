using System.Security.Cryptography;
using ApiRestProject.Application.Ports.Out;

namespace ApiRestProject.Infrastructure.Security;

/// <summary>
/// Hash de contraseñas con PBKDF2-SHA256 (sal aleatoria por contraseña).
/// Formato almacenado: "v1.iteraciones.salBase64.hashBase64".
/// </summary>
public class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const int Iterations = 210_000;
    private const int SaltSize = 16;
    private const int HashSize = 32;

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Derive(password, salt, Iterations);
        return $"v1.{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public bool Verify(string password, string? hash)
    {
        // Sin hash (usuario inexistente) se calcula igual para que el tiempo de respuesta no delate nada.
        if (!TryParse(hash, out var iterations, out var salt, out var expected))
        {
            Derive(password, new byte[SaltSize], Iterations);
            return false;
        }
        var actual = Derive(password, salt, iterations);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    private static byte[] Derive(string password, byte[] salt, int iterations) =>
        Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, HashSize);

    private static bool TryParse(string? stored, out int iterations, out byte[] salt, out byte[] hash)
    {
        iterations = 0; salt = []; hash = [];
        var parts = stored?.Split('.');
        if (parts is not { Length: 4 } || parts[0] != "v1") return false;
        try
        {
            iterations = int.Parse(parts[1]);
            salt = Convert.FromBase64String(parts[2]);
            hash = Convert.FromBase64String(parts[3]);
            return iterations > 0 && salt.Length > 0 && hash.Length == HashSize;
        }
        catch (FormatException) { return false; }
        catch (OverflowException) { return false; }
    }
}
