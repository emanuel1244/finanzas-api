using ApiRestProject.Application.Dtos;
using ApiRestProject.Application.Ports.In;
using ApiRestProject.Application.Ports.Out;
using ApiRestProject.Domain.Entities;

namespace ApiRestProject.Application.Services;

public class SavingsService(ISavingsPlanRepository repo) : ISavingsService
{
    private static DateOnly Today => DateOnly.FromDateTime(DateTime.Today);

    public async Task<List<SavingsPlanDto>> ListAsync(CancellationToken ct = default) =>
        (await repo.ListAsync(ct)).Select(s => s.ToDto(Today)).ToList();

    public async Task<SavingsPlanDto> GetAsync(int id, CancellationToken ct = default) =>
        (await Find(id, ct)).ToDto(Today);

    public async Task<SavingsPlanDto> CreateAsync(SavingsPlanRequest r, CancellationToken ct = default)
    {
        var s = SavingsPlan.Create(r.Name, r.TargetAmount, r.StartDate, r.TargetDate);
        await repo.AddAsync(s, ct);
        await repo.SaveChangesAsync(ct);
        return s.ToDto(Today);
    }

    public async Task<SavingsPlanDto> UpdateAsync(int id, SavingsPlanRequest r, CancellationToken ct = default)
    {
        var s = await Find(id, ct);
        s.Update(r.Name, r.TargetAmount, r.StartDate, r.TargetDate);
        await repo.SaveChangesAsync(ct);
        return s.ToDto(Today);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        repo.Remove(await Find(id, ct));
        await repo.SaveChangesAsync(ct);
    }

    public async Task<List<ContributionDto>> GetContributionsAsync(int id, CancellationToken ct = default) =>
        (await Find(id, ct)).Contributions.OrderBy(c => c.Date).ThenBy(c => c.Id).Select(c => c.ToDto()).ToList();

    public async Task<ContributionDto> AddContributionAsync(int id, ContributionRequest r, CancellationToken ct = default)
    {
        var s = await Find(id, ct);
        var c = s.AddContribution(r.Amount, r.Date, r.Note);
        await repo.SaveChangesAsync(ct);
        return c.ToDto();
    }

    private async Task<SavingsPlan> Find(int id, CancellationToken ct) =>
        await repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Plan de ahorro no encontrado.");
}
