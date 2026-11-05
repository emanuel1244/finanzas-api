using ApiRestProject.Application.Dtos;
using ApiRestProject.Application.Ports.In;
using Microsoft.AspNetCore.Mvc;

namespace ApiRestProject.Controllers;

[ApiController]
[Route("api/fixed-expenses")]
public class FixedExpensesController(IFixedExpenseService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<FixedExpenseDto>>> List(CancellationToken ct) => await service.ListAsync(ct);

    [HttpPost]
    public async Task<ActionResult<FixedExpenseDto>> Create(FixedExpenseRequest request, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await service.CreateAsync(request, ct));

    [HttpPut("{id:int}")]
    public async Task<ActionResult<FixedExpenseDto>> Update(int id, FixedExpenseRequest request, CancellationToken ct) =>
        await service.UpdateAsync(id, request, ct);

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return NoContent();
    }
}
