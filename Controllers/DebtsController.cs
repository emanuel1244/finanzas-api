using ApiRestProject.Application.Dtos;
using ApiRestProject.Application.Ports.In;
using Microsoft.AspNetCore.Mvc;

namespace ApiRestProject.Controllers;

[ApiController]
[Route("api/debts")]
public class DebtsController(IDebtService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<DebtDto>>> List(CancellationToken ct) => await service.ListAsync(ct);

    [HttpGet("{id:int}")]
    public async Task<ActionResult<DebtDto>> Get(int id, CancellationToken ct) => await service.GetAsync(id, ct);

    [HttpPost]
    public async Task<ActionResult<DebtDto>> Create(DebtRequest request, CancellationToken ct)
    {
        var created = await service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<DebtDto>> Update(int id, DebtUpdateRequest request, CancellationToken ct) =>
        await service.UpdateAsync(id, request, ct);

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return NoContent();
    }

    /// <summary>Tabla de amortización de lo que resta por pagar.</summary>
    [HttpGet("{id:int}/schedule")]
    public async Task<ActionResult<List<ScheduleRowDto>>> Schedule(int id, CancellationToken ct) =>
        await service.GetScheduleAsync(id, ct);

    [HttpGet("{id:int}/payments")]
    public async Task<ActionResult<List<DebtPaymentDto>>> Payments(int id, CancellationToken ct) =>
        await service.GetPaymentsAsync(id, ct);

    [HttpPost("{id:int}/payments")]
    public async Task<ActionResult<DebtPaymentDto>> RegisterPayment(int id, DebtPaymentRequest request, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await service.RegisterPaymentAsync(id, request, ct));
}
