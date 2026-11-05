using ApiRestProject.Application.Dtos;
using ApiRestProject.Application.Ports.In;
using Microsoft.AspNetCore.Mvc;

namespace ApiRestProject.Controllers;

[ApiController]
[Route("api/categories")]
public class CategoriesController(ICategoryService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<CategoryDto>>> List(CancellationToken ct) => await service.ListAsync(ct);

    [HttpPost]
    public async Task<ActionResult<CategoryDto>> Create(CategoryRequest request, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await service.CreateAsync(request, ct));

    [HttpPut("{id:int}")]
    public async Task<ActionResult<CategoryDto>> Update(int id, CategoryRequest request, CancellationToken ct) =>
        await service.UpdateAsync(id, request, ct);

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return NoContent();
    }
}
