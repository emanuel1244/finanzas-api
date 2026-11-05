using ApiRestProject.Application.Dtos;
using ApiRestProject.Application.Ports.In;
using ApiRestProject.Application.Ports.Out;
using ApiRestProject.Domain;
using ApiRestProject.Domain.Entities;

namespace ApiRestProject.Application.Services;

public static class PasswordPolicy
{
    /// <summary>Mínimo 8 caracteres, con al menos una letra y un número.</summary>
    public static void Validate(string password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < 8)
            throw new DomainException("La contraseña debe tener al menos 8 caracteres.");
        if (password.Length > 100)
            throw new DomainException("La contraseña no puede superar 100 caracteres.");
        if (!password.Any(char.IsLetter) || !password.Any(char.IsDigit))
            throw new DomainException("La contraseña debe incluir letras y números.");
    }
}

public class AuthService(IUserRepository users, IPasswordHasher hasher, ITokenService tokens, ICurrentUser current)
    : IAuthService
{
    public async Task<AuthResponse> LoginAsync(LoginRequest r, CancellationToken ct = default)
    {
        var user = await users.GetByEmailAsync(r.Email.Trim().ToLowerInvariant(), ct);

        // Se verifica siempre (aunque el usuario no exista) para no revelar por tiempo qué correos existen.
        var valid = hasher.Verify(r.Password, user?.PasswordHash);
        if (user is null || !valid)
            throw new UnauthorizedException("Correo o contraseña incorrectos.");
        if (!user.IsActive)
            throw new UnauthorizedException("Tu cuenta está desactivada. Contacta al administrador.");

        return Build(user);
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest r, CancellationToken ct = default)
    {
        PasswordPolicy.Validate(r.Password);
        var user = User.Create(r.Name, r.Email, hasher.Hash(r.Password), UserRole.Client);

        if (await users.EmailExistsAsync(user.Email, null, ct))
            throw new DomainException("Ya existe una cuenta con ese correo.");

        await users.AddAsync(user, ct);
        await users.SaveChangesAsync(ct);
        return Build(user);
    }

    public async Task<UserDto> GetCurrentAsync(CancellationToken ct = default) =>
        (await users.GetByIdAsync(current.UserId, ct) ?? throw new UnauthorizedException("Sesión no válida.")).ToDto();

    public async Task ChangePasswordAsync(ChangePasswordRequest r, CancellationToken ct = default)
    {
        var user = await users.GetByIdAsync(current.UserId, ct) ?? throw new UnauthorizedException("Sesión no válida.");
        if (!hasher.Verify(r.CurrentPassword, user.PasswordHash))
            throw new DomainException("La contraseña actual no es correcta.");

        PasswordPolicy.Validate(r.NewPassword);
        if (r.NewPassword == r.CurrentPassword)
            throw new DomainException("La nueva contraseña debe ser distinta de la actual.");

        user.SetPasswordHash(hasher.Hash(r.NewPassword));
        await users.SaveChangesAsync(ct);
    }

    private AuthResponse Build(User user)
    {
        var t = tokens.Create(user);
        return new AuthResponse(t.Token, t.ExpiresAt, user.ToDto());
    }
}

/// <summary>Administración de usuarios (solo rol Admin).</summary>
public class UserService(IUserRepository users, IPasswordHasher hasher, ICurrentUser current) : IUserService
{
    public async Task<List<UserDto>> ListAsync(CancellationToken ct = default) =>
        (await users.ListAsync(ct)).OrderBy(u => u.Name).Select(u => u.ToDto()).ToList();

    public async Task<UserDto> CreateAsync(UserCreateRequest r, CancellationToken ct = default)
    {
        PasswordPolicy.Validate(r.Password);
        var user = User.Create(r.Name, r.Email, hasher.Hash(r.Password), r.Role);

        if (await users.EmailExistsAsync(user.Email, null, ct))
            throw new DomainException("Ya existe una cuenta con ese correo.");

        await users.AddAsync(user, ct);
        await users.SaveChangesAsync(ct);
        return user.ToDto();
    }

    public async Task<UserDto> UpdateAsync(int id, UserUpdateRequest r, CancellationToken ct = default)
    {
        var user = await users.GetByIdAsync(id, ct) ?? throw new NotFoundException("Usuario no encontrado.");

        var losesAdminAccess = user.Role == UserRole.Admin && user.IsActive && (r.Role != UserRole.Admin || !r.IsActive);
        if (losesAdminAccess)
        {
            if (id == current.UserId)
                throw new DomainException("No puedes quitarte el rol de administrador ni desactivar tu propia cuenta.");
            if (await users.CountActiveAdminsAsync(id, ct) == 0)
                throw new DomainException("Debe quedar al menos un administrador activo.");
        }

        user.UpdateProfile(r.Name, r.Role, r.IsActive);
        await users.SaveChangesAsync(ct);
        return user.ToDto();
    }

    public async Task ResetPasswordAsync(int id, ResetPasswordRequest r, CancellationToken ct = default)
    {
        var user = await users.GetByIdAsync(id, ct) ?? throw new NotFoundException("Usuario no encontrado.");
        PasswordPolicy.Validate(r.NewPassword);
        user.SetPasswordHash(hasher.Hash(r.NewPassword));
        await users.SaveChangesAsync(ct);
    }
}
