using System.Security.Claims;
using System.Text;
using ApiRestProject.Application.Ports.Out;
using ApiRestProject.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace ApiRestProject.Infrastructure.Security;

public class JwtOptions
{
    public string Issuer { get; set; } = "FinanzasApi";
    public string Audience { get; set; } = "FinanzasWeb";
    /// <summary>Clave de firma (mínimo 32 caracteres). En producción defínela por variable de entorno Jwt__Key.</summary>
    public string Key { get; set; } = string.Empty;
    public int ExpiresMinutes { get; set; } = 480;

    public SymmetricSecurityKey SigningKey()
    {
        if (Encoding.UTF8.GetByteCount(Key) < 32)
            throw new InvalidOperationException("La clave 'Jwt:Key' debe tener al menos 32 caracteres.");
        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Key));
    }
}

public class JwtTokenService(IOptions<JwtOptions> options) : ITokenService
{
    private readonly JwtOptions _opt = options.Value;

    public TokenResult Create(User user)
    {
        var expires = DateTime.UtcNow.AddMinutes(_opt.ExpiresMinutes);
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _opt.Issuer,
            Audience = _opt.Audience,
            Expires = expires,
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("name", user.Name),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
            ]),
            SigningCredentials = new SigningCredentials(_opt.SigningKey(), SecurityAlgorithms.HmacSha256),
        };
        return new TokenResult(new JsonWebTokenHandler().CreateToken(descriptor), expires);
    }
}
