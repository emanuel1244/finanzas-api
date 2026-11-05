using ApiRestProject.Application.Dtos;
using ApiRestProject.Application.Ports.In;
using ApiRestProject.Application.Ports.Out;
using ApiRestProject.Domain.Entities;

namespace ApiRestProject.Application.Services;

public class DebtService(IDebtRepository repo) : IDebtService
{
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
        await repo.SaveChangesAsync(ct);
        return payment.ToDto();
    }

    private async Task<Debt> Find(int id, CancellationToken ct) =>
        await repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Deuda no encontrada.");
}
