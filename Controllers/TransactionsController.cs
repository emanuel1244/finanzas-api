using ApiRestProject.Application.Dtos;
using ApiRestProject.Application.Ports.In;
using ApiRestProject.Domain;
using Microsoft.AspNetCore.Mvc;

namespace ApiRestProject.Controllers;

[ApiController]
[Route("api/transactions")]
public class TransactionsController(ITransactionService service) : ControllerBase
{
    /// <summary>Filtros opcionales: from, to (yyyy-MM-dd), type (Income|Expense), categoryId.</summary>
    [HttpGet]
    public async Task<ActionResult<List<TransactionDto>>> Search([FromQuery] DateOnly? from, [FromQuery] DateOnly? to,
        [FromQuery] TransactionType? type, [FromQuery] int? categoryId, CancellationToken ct) =>
        await service.SearchAsync(from, to, type, categoryId, ct);

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TransactionDto>> Get(int id, CancellationToken ct) => await service.GetAsync(id, ct);

    [HttpPost]
    public async Task<ActionResult<TransactionDto>> Create(TransactionRequest request, CancellationToken ct)
    {
        var created = await service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<TransactionDto>> Update(int id, TransactionRequest request, CancellationToken ct) =>
        await service.UpdateAsync(id, request, ct);

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return NoContent();
    }
}
