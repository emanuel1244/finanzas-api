using ApiRestProject.Application.Dtos;
using ApiRestProject.Application.Ports.In;
using ApiRestProject.Application.Ports.Out;
using ApiRestProject.Domain;

namespace ApiRestProject.Application.Services;

/// <summary>
/// Calcula cuánto te queda disponible en el mes:
/// Ingresos - gastos fijos - gastos variables - deudas - ahorro.
/// Para deudas y ahorro se toma el mayor entre lo ya pagado/aportado y lo comprometido,
/// así el disponible refleja lo que realmente puedes gastar.
/// </summary>
public class DashboardService(
    ITransactionRepository transactions,
    IFixedExpenseRepository fixedExpenses,
    IDebtRepository debts,
    ISavingsPlanRepository savings) : IDashboardService
{
    public async Task<DashboardDto> GetMonthlySummaryAsync(int year, int month, CancellationToken ct = default)
    {
        if (year is < 2000 or > 2100 || month is < 1 or > 12)
            throw new DomainException("Año o mes no válidos.");

        var from = new DateOnly(year, month, 1);
        var to = from.AddMonths(1).AddDays(-1);

        var monthTx = await transactions.SearchAsync(from, to, null, null, ct);
        var income = monthTx.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
        var variable = monthTx.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);

        var fixedTotal = (await fixedExpenses.ListAsync(ct)).Where(f => f.IsActive).Sum(f => f.Amount);

        decimal debtOutflow = 0;
        foreach (var d in await debts.ListAsync(ct))
        {
            var paid = d.Payments.Where(p => p.Date >= from && p.Date <= to).Sum(p => p.Amount);
            var committed = d.Status == DebtStatus.Active && d.StartDate <= to ? d.MonthlyCommitment : 0;
            debtOutflow += Math.Max(paid, committed);
        }

        decimal savingsOutflow = 0;
        foreach (var s in await savings.ListAsync(ct))
        {
            var contributed = s.Contributions.Where(c => c.Date >= from && c.Date <= to).Sum(c => c.Amount);
            var planned = s.Status == SavingsStatus.Active && s.StartDate <= to
                ? s.SuggestedMonthlyContribution(from) : 0;
            savingsOutflow += Math.Max(contributed, planned);
        }

        var byCategory = monthTx
            .Where(t => t.Type == TransactionType.Expense)
            .GroupBy(t => t.Category?.Name ?? "Sin categoría")
            .Select(g => new CategoryTotalDto(g.Key, g.Sum(t => t.Amount)))
            .OrderByDescending(x => x.Total)
            .ToList();

        var totalOutflow = fixedTotal + variable + debtOutflow + savingsOutflow;
        return new DashboardDto(year, month, income, fixedTotal, variable, debtOutflow, savingsOutflow,
            totalOutflow, income - totalOutflow, byCategory);
    }
}
