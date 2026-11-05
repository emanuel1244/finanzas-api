using ApiRestProject.Application.Dtos;
using ApiRestProject.Application.Ports.In;
using Microsoft.AspNetCore.Mvc;

namespace ApiRestProject.Controllers;

[ApiController]
[Route("api/budgets")]
public class BudgetsController(IBudgetService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<BudgetDto>>> List(CancellationToken ct) => await service.ListAsync(ct);

    /// <summary>Avance de cada presupuesto en un mes. Sin parámetros usa el mes actual.</summary>
    [HttpGet("status")]
    public async Task<ActionResult<List<BudgetStatusDto>>> Status([FromQuery] int? year, [FromQuery] int? month, CancellationToken ct)
    {
        var today = DateTime.Today;
        return await service.GetStatusAsync(year ?? today.Year, month ?? today.Month, ct);
    }

    [HttpPost]
    public async Task<ActionResult<BudgetDto>> Create(BudgetRequest request, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await service.CreateAsync(request, ct));

    [HttpPut("{id:int}")]
    public async Task<ActionResult<BudgetDto>> Update(int id, BudgetRequest request, CancellationToken ct) =>
        await service.UpdateAsync(id, request, ct);

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return NoContent();
    }
}
