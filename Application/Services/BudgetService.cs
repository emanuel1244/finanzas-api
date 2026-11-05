using ApiRestProject.Application.Dtos;
using ApiRestProject.Application.Ports.In;
using ApiRestProject.Application.Ports.Out;
using ApiRestProject.Domain;
using ApiRestProject.Domain.Entities;

namespace ApiRestProject.Application.Services;

/// <summary>
/// Presupuestos mensuales por categoría de gasto. Lo gastado en el mes suma las transacciones
/// de gasto de la categoría más los gastos fijos activos asignados a ella.
/// </summary>
public class BudgetService(
    IBudgetRepository repo,
    ICategoryRepository categories,
    ITransactionRepository transactions,
    IFixedExpenseRepository fixedExpenses) : IBudgetService
{
    private const decimal WarningThreshold = 80m;

    public async Task<List<BudgetDto>> ListAsync(CancellationToken ct = default) =>
        (await repo.ListAsync(ct)).Select(b => b.ToDto()).ToList();

    public async Task<List<BudgetStatusDto>> GetStatusAsync(int year, int month, CancellationToken ct = default)
    {
        if (year is < 2000 or > 2100 || month is < 1 or > 12)
            throw new DomainException("Año o mes no válidos.");

        var from = new DateOnly(year, month, 1);
        var to = from.AddMonths(1).AddDays(-1);

        var spentByCategory = (await transactions.SearchAsync(from, to, TransactionType.Expense, null, ct))
            .GroupBy(t => t.CategoryId)
            .ToDictionary(g => g.Key, g => g.Sum(t => t.Amount));

        foreach (var f in (await fixedExpenses.ListAsync(ct)).Where(f => f.IsActive && f.CategoryId is not null))
            spentByCategory[f.CategoryId!.Value] = spentByCategory.GetValueOrDefault(f.CategoryId.Value) + f.Amount;

        return (await repo.ListAsync(ct))
            .Select(b =>
            {
                var spent = spentByCategory.GetValueOrDefault(b.CategoryId);
                var percent = Math.Round(spent / b.MonthlyLimit * 100m, 2);
                var state = percent > 100m ? BudgetState.Exceeded
                    : percent >= WarningThreshold ? BudgetState.Warning
                    : BudgetState.Ok;
                return new BudgetStatusDto(b.Id, b.CategoryId, b.Category?.Name ?? string.Empty,
                    b.MonthlyLimit, spent, b.MonthlyLimit - spent, percent, state);
            })
            .OrderByDescending(s => s.PercentUsed)
            .ToList();
    }

    public async Task<BudgetDto> CreateAsync(BudgetRequest r, CancellationToken ct = default)
    {
        var category = await GetCategoryAsync(r.CategoryId, ct);
        await EnsureUniqueAsync(r.CategoryId, null, ct);
        var b = Budget.Create(category, r.MonthlyLimit);
        await repo.AddAsync(b, ct);
        await repo.SaveChangesAsync(ct);
        return b.ToDto();
    }

    public async Task<BudgetDto> UpdateAsync(int id, BudgetRequest r, CancellationToken ct = default)
    {
        var b = await repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Presupuesto no encontrado.");
        var category = await GetCategoryAsync(r.CategoryId, ct);
        await EnsureUniqueAsync(r.CategoryId, id, ct);
        b.Update(category, r.MonthlyLimit);
        await repo.SaveChangesAsync(ct);
        return b.ToDto();
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var b = await repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Presupuesto no encontrado.");
        repo.Remove(b);
        await repo.SaveChangesAsync(ct);
    }

    private async Task<Category> GetCategoryAsync(int categoryId, CancellationToken ct) =>
        await categories.GetByIdAsync(categoryId, ct) ?? throw new DomainException("La categoría indicada no existe.");

    private async Task EnsureUniqueAsync(int categoryId, int? excludingId, CancellationToken ct)
    {
        if (await repo.ExistsForCategoryAsync(categoryId, excludingId, ct))
            throw new DomainException("Ya existe un presupuesto para esa categoría.");
    }
}
