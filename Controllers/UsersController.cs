using ApiRestProject.Application.Dtos;
using ApiRestProject.Application.Ports.In;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiRestProject.Controllers;

/// <summary>Administración de usuarios: solo para el rol Admin.</summary>
[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/users")]
public class UsersController(IUserService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<UserDto>>> List(CancellationToken ct) => await service.ListAsync(ct);

    [HttpPost]
    public async Task<ActionResult<UserDto>> Create(UserCreateRequest request, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await service.CreateAsync(request, ct));

    [HttpPut("{id:int}")]
    public async Task<ActionResult<UserDto>> Update(int id, UserUpdateRequest request, CancellationToken ct) =>
        await service.UpdateAsync(id, request, ct);

    [HttpPost("{id:int}/reset-password")]
    public async Task<IActionResult> ResetPassword(int id, ResetPasswordRequest request, CancellationToken ct)
    {
        await service.ResetPasswordAsync(id, request, ct);
        return NoContent();
    }
}
