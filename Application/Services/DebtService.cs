using ApiRestProject.Application.Dtos;
using ApiRestProject.Application.Ports.In;
using ApiRestProject.Application.Ports.Out;
using ApiRestProject.Domain;
using ApiRestProject.Domain.Entities;

namespace ApiRestProject.Application.Services;

public class DebtService(
    IDebtRepository repo,
    ITransactionRepository transactions,
    ICategoryRepository categories) : IDebtService
{
    /// <summary>Categoría de ingreso donde se registra el saldo a favor por pagos parciales de deuda.</summary>
    private const string ShortfallCategoryName = "Saldo a favor deuda";

    public async Task<List<DebtDto>> ListAsync(CancellationToken ct = default) =>
        (await repo.ListAsync(ct)).Select(d => d.ToDto()).ToList();

    public async Task<DebtDto> GetAsync(int id, CancellationToken ct = default) =>
        (await Find(id, ct)).ToDto();

    public async Task<DebtDto> CreateAsync(DebtRequest r, CancellationToken ct = default)
    {
        var d = Debt.Create(r.Name, r.Creditor, r.Principal, r.InterestRatePerPeriod, r.Period, r.TotalPeriods, r.StartDate, r.AmortizationSystem);
        await repo.AddAsync(d, ct);
        await repo.SaveChangesAsync(ct);
        return d.ToDto();
    }

    public async Task<DebtDto> UpdateAsync(int id, DebtUpdateRequest r, CancellationToken ct = default)
    {
        var d = await Find(id, ct);
        d.UpdateInfo(r.Name, r.Creditor);
        await repo.SaveChangesAsync(ct);
        return d.ToDto();
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        repo.Remove(await Find(id, ct));
        await repo.SaveChangesAsync(ct);
    }

    public async Task<List<ScheduleRowDto>> GetScheduleAsync(int id, CancellationToken ct = default) =>
        (await Find(id, ct)).BuildSchedule().Select(r => r.ToDto()).ToList();

    public async Task<List<DebtPaymentDto>> GetPaymentsAsync(int id, CancellationToken ct = default) =>
        (await Find(id, ct)).Payments.OrderBy(p => p.Date).ThenBy(p => p.Id).Select(p => p.ToDto()).ToList();

    public async Task<DebtPaymentDto> RegisterPaymentAsync(int id, DebtPaymentRequest r, CancellationToken ct = default)
    {
        var d = await Find(id, ct);
        var payment = d.RegisterPayment(r.Amount, r.Date);

        // Si el abono fue menor que la cuota del periodo, ese faltante es dinero que quedó
        // disponible en el mes: se registra como un ingreso ("saldo a favor deuda").
        if (payment.IsPartial)
        {
            var category = await EnsureShortfallCategoryAsync(ct);
            var income = Transaction.Create(category, payment.Shortfall, r.Date,
                $"Saldo a favor por pago parcial de la deuda '{d.Name}'.");
            await transactions.AddAsync(income, ct);
        }

        await repo.SaveChangesAsync(ct);
        return payment.ToDto();
    }

    /// <summary>Obtiene (o crea) la categoría de ingreso usada para el saldo a favor de deudas.</summary>
    private async Task<Category> EnsureShortfallCategoryAsync(CancellationToken ct)
    {
        var existing = (await categories.ListAsync(ct))
            .FirstOrDefault(c => c.Type == TransactionType.Income && c.Name == ShortfallCategoryName);
        if (existing is not null) return existing;

        var category = Category.Create(ShortfallCategoryName, TransactionType.Income);
        await categories.AddAsync(category, ct);
        return category;
    }

    private async Task<Debt> Find(int id, CancellationToken ct) =>
        await repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Deuda no encontrada.");
}
