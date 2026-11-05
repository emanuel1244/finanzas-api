using System.Security.Claims;
using ApiRestProject.Application.Ports.Out;
using ApiRestProject.Domain;

namespace ApiRestProject.Api;

/// <summary>Obtiene el usuario autenticado a partir de los claims del JWT.</summary>
public class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public int UserId
    {
        get
        {
            var value = Principal?.FindFirstValue("sub") ?? Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(value, out var id) ? id : 0;
        }
    }

    public bool IsAdmin => Principal?.IsInRole(UserRole.Admin.ToString()) ?? false;
}
