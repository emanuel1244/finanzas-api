using ApiRestProject.Application.Dtos;
using ApiRestProject.Application.Ports.In;
using Microsoft.AspNetCore.Mvc;

namespace ApiRestProject.Controllers;

[ApiController]
[Route("api/savings-plans")]
public class SavingsPlansController(ISavingsService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<SavingsPlanDto>>> List(CancellationToken ct) => await service.ListAsync(ct);

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SavingsPlanDto>> Get(int id, CancellationToken ct) => await service.GetAsync(id, ct);

    [HttpPost]
    public async Task<ActionResult<SavingsPlanDto>> Create(SavingsPlanRequest request, CancellationToken ct)
    {
        var created = await service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<SavingsPlanDto>> Update(int id, SavingsPlanRequest request, CancellationToken ct) =>
        await service.UpdateAsync(id, request, ct);

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return NoContent();
    }

    [HttpGet("{id:int}/contributions")]
    public async Task<ActionResult<List<ContributionDto>>> Contributions(int id, CancellationToken ct) =>
        await service.GetContributionsAsync(id, ct);

    [HttpPost("{id:int}/contributions")]
    public async Task<ActionResult<ContributionDto>> AddContribution(int id, ContributionRequest request, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await service.AddContributionAsync(id, request, ct));
}
