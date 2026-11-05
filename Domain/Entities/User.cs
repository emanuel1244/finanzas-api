namespace ApiRestProject.Domain.Entities;

/// <summary>Usuario del sistema: administrador o cliente.</summary>
public class User : Entity
{
    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; }

    private User() { }

    public static User Create(string name, string email, string passwordHash, UserRole role)
    {
        var u = new User { CreatedAt = DateTime.UtcNow };
        u.UpdateProfile(name, role, true);
        u.SetEmail(email);
        u.SetPasswordHash(passwordHash);
        return u;
    }

    public void UpdateProfile(string name, UserRole role, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("El nombre es obligatorio.");
        if (!Enum.IsDefined(role)) throw new DomainException("Rol no válido.");
        Name = name.Trim();
        Role = role;
        IsActive = isActive;
    }

    public void SetEmail(string email)
    {
        var e = (email ?? string.Empty).Trim().ToLowerInvariant();
        if (e.Length is 0 or > 254 || !e.Contains('@') || e.StartsWith('@') || e.EndsWith('@'))
            throw new DomainException("El correo electrónico no es válido.");
        Email = e;
    }

    public void SetPasswordHash(string hash)
    {
        if (string.IsNullOrWhiteSpace(hash)) throw new DomainException("Contraseña no válida.");
        PasswordHash = hash;
    }
}
