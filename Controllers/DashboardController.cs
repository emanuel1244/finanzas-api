using ApiRestProject.Application.Dtos;
using ApiRestProject.Application.Ports.In;
using Microsoft.AspNetCore.Mvc;

namespace ApiRestProject.Controllers;

[ApiController]
[Route("api/dashboard")]
public class DashboardController(IDashboardService service) : ControllerBase
{
    /// <summary>Resumen mensual y dinero disponible. Sin parámetros usa el mes actual.</summary>
    [HttpGet]
    public async Task<ActionResult<DashboardDto>> Get([FromQuery] int? year, [FromQuery] int? month, CancellationToken ct)
    {
        var today = DateTime.Today;
        return await service.GetMonthlySummaryAsync(year ?? today.Year, month ?? today.Month, ct);
    }
}
