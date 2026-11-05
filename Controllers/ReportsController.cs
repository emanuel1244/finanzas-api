using ApiRestProject.Application.Dtos;
using ApiRestProject.Application.Ports.In;
using Microsoft.AspNetCore.Mvc;

namespace ApiRestProject.Controllers;

[ApiController]
[Route("api/reports")]
public class ReportsController(IReportService service) : ControllerBase
{
    /// <summary>Ingresos vs gastos de los últimos N meses (por defecto 6).</summary>
    [HttpGet("monthly-trend")]
    public async Task<ActionResult<List<MonthlyTrendDto>>> MonthlyTrend([FromQuery] int months = 6, CancellationToken ct = default) =>
        await service.GetMonthlyTrendAsync(months, DateOnly.FromDateTime(DateTime.Today), ct);

    /// <summary>Gasto por categoría en un rango. Sin parámetros usa el mes actual.</summary>
    [HttpGet("expenses-by-category")]
    public async Task<ActionResult<List<CategoryBreakdownDto>>> ExpensesByCategory(
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var start = from ?? new DateOnly(today.Year, today.Month, 1);
        var end = to ?? start.AddMonths(1).AddDays(-1);
        return await service.GetExpensesByCategoryAsync(start, end, ct);
    }
}
