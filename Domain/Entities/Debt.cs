namespace ApiRestProject.Domain.Entities;

/// <summary>
/// Deuda con interés porcentual por periodo. Soporta tres sistemas de cálculo de cuotas:
/// francés (cuota fija), alemán (capital constante, cuota decreciente) e interés simple.
/// Ej.: 2% mensual, 12 periodos mensuales.
/// </summary>
public class Debt : Entity, IOwned
{
    private readonly List<DebtPayment> _payments = [];

    public int UserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Creditor { get; private set; }
    public decimal Principal { get; private set; }
    /// <summary>Porcentaje de interés por periodo (2 = 2%).</summary>
    public decimal InterestRatePerPeriod { get; private set; }
    public PeriodType Period { get; private set; }
    public int TotalPeriods { get; private set; }
    public DateOnly StartDate { get; private set; }
    public decimal Balance { get; private set; }
    public DebtStatus Status { get; private set; } = DebtStatus.Active;
    /// <summary>Sistema de cálculo de cuotas aplicado a esta deuda.</summary>
    public AmortizationSystem AmortizationSystem { get; private set; } = AmortizationSystem.French;
    public IReadOnlyCollection<DebtPayment> Payments => _payments;

    private Debt() { }

    public static Debt Create(string name, string? creditor, decimal principal, decimal ratePerPeriod,
        PeriodType period, int totalPeriods, DateOnly startDate,
        AmortizationSystem amortizationSystem = AmortizationSystem.French)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("El nombre es obligatorio.");
        if (principal <= 0) throw new DomainException("El monto de la deuda debe ser mayor a cero.");
        if (ratePerPeriod < 0) throw new DomainException("El interés no puede ser negativo.");
        if (totalPeriods < 1) throw new DomainException("La deuda debe tener al menos 1 periodo.");
        if (!Enum.IsDefined(period)) throw new DomainException("Periodo no válido.");
        if (!Enum.IsDefined(amortizationSystem)) throw new DomainException("Sistema de amortización no válido.");

        return new Debt
        {
            Name = name.Trim(),
            Creditor = string.IsNullOrWhiteSpace(creditor) ? null : creditor.Trim(),
            Principal = principal,
            InterestRatePerPeriod = ratePerPeriod,
            Period = period,
            TotalPeriods = totalPeriods,
            StartDate = startDate,
            Balance = principal,
            AmortizationSystem = amortizationSystem
        };
    }

    public void UpdateInfo(string name, string? creditor)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("El nombre es obligatorio.");
        Name = name.Trim();
        Creditor = string.IsNullOrWhiteSpace(creditor) ? null : creditor.Trim();
    }

    public int RemainingPeriods => Math.Max(1, TotalPeriods - _payments.Count);

    public decimal TotalInterestPaid => _payments.Sum(p => p.Interest);

    /// <summary>Cuota calculada para el próximo pago según el sistema de amortización.</summary>
    public decimal CurrentInstallment =>
        Status == DebtStatus.Paid ? 0 : NextInstallment();

    /// <summary>Cuota expresada en equivalente mensual (para calcular lo disponible del mes).</summary>
    public decimal MonthlyCommitment =>
        Math.Round(CurrentInstallment * Period.PeriodsPerYear() / 12m, 2);

    /// <summary>Interés del próximo pago según el sistema de amortización.</summary>
    public decimal InterestForNextPayment => AmortizationSystem switch
    {
        // El interés simple se calcula siempre sobre el capital original.
        AmortizationSystem.SimpleInterest => Round(Principal * InterestRatePerPeriod / 100m),
        // Francés y alemán calculan el interés sobre el saldo pendiente.
        _ => Round(Balance * InterestRatePerPeriod / 100m)
    };

    public DebtPayment RegisterPayment(decimal amount, DateOnly date)
    {
        if (Status == DebtStatus.Paid) throw new DomainException("La deuda ya está pagada.");
        if (amount <= 0) throw new DomainException("El monto debe ser mayor a cero.");

        var interest = InterestForNextPayment;
        if (amount < interest)
            throw new DomainException($"El pago no cubre el interés del periodo ({interest:N2}).");
        if (amount > Balance + interest)
            throw new DomainException($"El pago excede el saldo pendiente más intereses ({Balance + interest:N2}).");

        // Cuota esperada del periodo (antes de abonar). Si el abono es menor, el faltante
        // se registra como "saldo a favor del mes": capital que no se amortizó y que queda
        // disponible en el flujo de caja. El saldo baja solo por lo realmente abonado, así
        // que las cuotas de los periodos restantes se recalculan al alza (mismo plazo).
        var installmentDue = Round(CurrentInstallment);
        var shortfall = installmentDue > amount ? Round(installmentDue - amount) : 0m;

        var capital = amount - interest;
        Balance -= capital;
        if (Balance <= 0)
        {
            Balance = 0;
            Status = DebtStatus.Paid;
        }

        var payment = new DebtPayment(date, amount, interest, capital, Balance, installmentDue, shortfall);
        _payments.Add(payment);
        return payment;
    }

    /// <summary>Tabla de amortización de lo que falta por pagar, según el sistema de amortización.</summary>
    public IReadOnlyList<ScheduleRow> BuildSchedule()
    {
        var rows = new List<ScheduleRow>();
        if (Status == DebtStatus.Paid) return rows;

        var n = RemainingPeriods;
        var balance = Balance;
        var paidSoFar = _payments.Count;

        // Capital constante para alemán e interés simple.
        var fixedCapital = Round(balance / n);
        // Cuota fija para francés y para interés simple.
        var frenchInstallment = CalculateFrenchInstallment(balance, InterestRatePerPeriod, n);
        var simpleInterest = Round(Principal * InterestRatePerPeriod / 100m);

        for (var i = 1; i <= n; i++)
        {
            decimal interest, capital;
            switch (AmortizationSystem)
            {
                case AmortizationSystem.German:
                    interest = Round(balance * InterestRatePerPeriod / 100m);
                    capital = i == n ? balance : Math.Min(fixedCapital, balance);
                    break;

                case AmortizationSystem.SimpleInterest:
                    interest = i == n ? Round(balance * InterestRatePerPeriod / 100m) : simpleInterest;
                    capital = i == n ? balance : Math.Min(fixedCapital, balance);
                    break;

                default: // French
                    interest = Round(balance * InterestRatePerPeriod / 100m);
                    capital = i == n ? balance : Math.Min(frenchInstallment - interest, balance);
                    break;
            }

            balance -= capital;
            rows.Add(new ScheduleRow(i, Period.AddPeriods(StartDate, paidSoFar + i),
                capital + interest, interest, capital, balance));
        }
        return rows;
    }

    /// <summary>Cuota del próximo pago según el sistema (para <see cref="CurrentInstallment"/>).</summary>
    private decimal NextInstallment()
    {
        var n = RemainingPeriods;
        return AmortizationSystem switch
        {
            // Capital constante + interés sobre saldo.
            AmortizationSystem.German => Round(Balance / n) + InterestForNextPayment,
            // Capital constante + interés sobre capital original.
            AmortizationSystem.SimpleInterest => Round(Balance / n) + InterestForNextPayment,
            // Cuota fija francesa.
            _ => CalculateFrenchInstallment(Balance, InterestRatePerPeriod, n)
        };
    }

    private static decimal CalculateFrenchInstallment(decimal balance, decimal ratePct, int periods)
    {
        if (ratePct == 0) return Round(balance / periods);
        var r = (double)ratePct / 100d;
        var value = (double)balance * r / (1d - Math.Pow(1d + r, -periods));
        return Round((decimal)value);
    }

    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}

public class DebtPayment : Entity
{
    public int DebtId { get; private set; }
    public DateOnly Date { get; private set; }
    public decimal Amount { get; private set; }
    public decimal Interest { get; private set; }
    public decimal Capital { get; private set; }
    public decimal BalanceAfter { get; private set; }
    /// <summary>Cuota esperada del periodo al momento del pago.</summary>
    public decimal InstallmentDue { get; private set; }
    /// <summary>Faltante respecto a la cuota esperada (saldo a favor del mes). 0 si se pagó completa o de más.</summary>
    public decimal Shortfall { get; private set; }
    /// <summary>Indica si el abono fue menor que la cuota esperada.</summary>
    public bool IsPartial => Shortfall > 0;

    private DebtPayment() { }

    internal DebtPayment(DateOnly date, decimal amount, decimal interest, decimal capital, decimal balanceAfter,
        decimal installmentDue, decimal shortfall)
    {
        Date = date;
        Amount = amount;
        Interest = interest;
        Capital = capital;
        BalanceAfter = balanceAfter;
        InstallmentDue = installmentDue;
        Shortfall = shortfall;
    }
}

public record ScheduleRow(int Number, DateOnly DueDate, decimal Installment, decimal Interest, decimal Capital, decimal Balance);
