using ApiRestProject.Application.Dtos;
using ApiRestProject.Application.Ports.In;
using ApiRestProject.Application.Ports.Out;
using ApiRestProject.Domain;

namespace ApiRestProject.Application.Services;

/// <summary>Reportes agregados: tendencia mensual y gasto por categoría.</summary>
public class ReportService(
    ITransactionRepository transactions,
    IFixedExpenseRepository fixedExpenses) : IReportService
{
    public async Task<List<MonthlyTrendDto>> GetMonthlyTrendAsync(int months, DateOnly today, CancellationToken ct = default)
    {
        if (months is < 1 or > 24) throw new DomainException("El número de meses debe estar entre 1 y 24.");

        var lastMonth = new DateOnly(today.Year, today.Month, 1);
        var firstMonth = lastMonth.AddMonths(-(months - 1));
        var to = lastMonth.AddMonths(1).AddDays(-1);

        var txs = await transactions.SearchAsync(firstMonth, to, null, null, ct);
        var fixedTotal = (await fixedExpenses.ListAsync(ct)).Where(f => f.IsActive).Sum(f => f.Amount);

        var result = new List<MonthlyTrendDto>(months);
        for (var i = 0; i < months; i++)
        {
            var m = firstMonth.AddMonths(i);
            var monthTx = txs.Where(t => t.Date.Year == m.Year && t.Date.Month == m.Month).ToList();
            var income = monthTx.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
            var variable = monthTx.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);
            var total = variable + fixedTotal;
            result.Add(new MonthlyTrendDto(m.Year, m.Month, income, fixedTotal, variable, total, income - total));
        }
        return result;
    }

    public async Task<List<CategoryBreakdownDto>> GetExpensesByCategoryAsync(DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        if (to < from) throw new DomainException("La fecha final no puede ser anterior a la inicial.");

        var groups = (await transactions.SearchAsync(from, to, TransactionType.Expense, null, ct))
            .GroupBy(t => t.Category?.Name ?? "Sin categoría")
            .Select(g => (Category: g.Key, Total: g.Sum(t => t.Amount)))
            .OrderByDescending(x => x.Total)
            .ToList();

        var grand = groups.Sum(x => x.Total);
        return groups
            .Select(x => new CategoryBreakdownDto(x.Category, x.Total, grand == 0 ? 0 : Math.Round(x.Total / grand * 100m, 2)))
            .ToList();
    }
}
