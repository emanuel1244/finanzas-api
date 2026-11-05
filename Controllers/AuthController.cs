using ApiRestProject.Application.Dtos;
using ApiRestProject.Application.Ports.In;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ApiRestProject.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService service, IConfiguration config) : ControllerBase
{
    /// <summary>Inicia sesión y devuelve un token JWT.</summary>
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken ct) =>
        await service.LoginAsync(request, ct);

    /// <summary>Crea una cuenta con rol Cliente (se puede desactivar con Auth:AllowRegistration=false).</summary>
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken ct)
    {
        if (!config.GetValue("Auth:AllowRegistration", true))
            return StatusCode(StatusCodes.Status403Forbidden,
                new ProblemDetails { Status = 403, Title = "Registro deshabilitado", Detail = "El registro de cuentas está deshabilitado." });

        return StatusCode(StatusCodes.Status201Created, await service.RegisterAsync(request, ct));
    }

    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> Me(CancellationToken ct) => await service.GetCurrentAsync(ct);

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken ct)
    {
        await service.ChangePasswordAsync(request, ct);
        return NoContent();
    }
}
