namespace ApiRestProject.Domain.Entities;

/// <summary>Plan de ahorro con meta y fecha objetivo.</summary>
public class SavingsPlan : Entity, IOwned
{
    private readonly List<SavingsContribution> _contributions = [];

    public int UserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public decimal TargetAmount { get; private set; }
    public decimal CurrentAmount { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly TargetDate { get; private set; }
    public SavingsStatus Status { get; private set; } = SavingsStatus.Active;
    public IReadOnlyCollection<SavingsContribution> Contributions => _contributions;

    private SavingsPlan() { }

    public static SavingsPlan Create(string name, decimal targetAmount, DateOnly startDate, DateOnly targetDate)
    {
        var p = new SavingsPlan();
        p.Update(name, targetAmount, startDate, targetDate);
        return p;
    }

    public void Update(string name, decimal targetAmount, DateOnly startDate, DateOnly targetDate)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("El nombre es obligatorio.");
        if (targetAmount <= 0) throw new DomainException("La meta debe ser mayor a cero.");
        if (targetDate <= startDate) throw new DomainException("La fecha objetivo debe ser posterior a la de inicio.");
        Name = name.Trim();
        TargetAmount = targetAmount;
        StartDate = startDate;
        TargetDate = targetDate;
        RefreshStatus();
    }

    public decimal ProgressPercent => Math.Round(Math.Min(100m, CurrentAmount / TargetAmount * 100m), 2);

    /// <summary>Aporte mensual sugerido para llegar a la meta a tiempo.</summary>
    public decimal SuggestedMonthlyContribution(DateOnly asOf)
    {
        var remaining = TargetAmount - CurrentAmount;
        if (remaining <= 0) return 0;
        var months = (TargetDate.Year - asOf.Year) * 12 + TargetDate.Month - asOf.Month + 1;
        months = Math.Max(1, months);
        return Math.Round(remaining / months, 2, MidpointRounding.AwayFromZero);
    }

    public SavingsContribution AddContribution(decimal amount, DateOnly date, string? note)
    {
        if (amount <= 0) throw new DomainException("El aporte debe ser mayor a cero.");
        var c = new SavingsContribution(date, amount, string.IsNullOrWhiteSpace(note) ? null : note.Trim());
        _contributions.Add(c);
        CurrentAmount += amount;
        RefreshStatus();
        return c;
    }

    private void RefreshStatus() =>
        Status = CurrentAmount >= TargetAmount ? SavingsStatus.Completed : SavingsStatus.Active;
}

public class SavingsContribution : Entity
{
    public int SavingsPlanId { get; private set; }
    public DateOnly Date { get; private set; }
    public decimal Amount { get; private set; }
    public string? Note { get; private set; }

    private SavingsContribution() { }

    internal SavingsContribution(DateOnly date, decimal amount, string? note)
    {
        Date = date;
        Amount = amount;
        Note = note;
    }
}
