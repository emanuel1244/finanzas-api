namespace ApiRestProject.Domain;

public abstract class Entity
{
    public int Id { get; protected set; }
}

/// <summary>Error de regla de negocio (se traduce a HTTP 400).</summary>
public class DomainException(string message) : Exception(message);

/// <summary>Entidad que pertenece a un usuario; sus consultas se filtran por el usuario actual.</summary>
public interface IOwned
{
    int UserId { get; }
}

public enum UserRole
{
    Admin = 1,
    Client = 2
}

public enum TransactionType
{
    Income = 1,
    Expense = 2
}

/// <summary>Periodicidad usada por deudas (la tasa de interés es "por periodo").</summary>
public enum PeriodType
{
    Weekly = 1,
    Biweekly = 2,
    Monthly = 3,
    Quarterly = 4,
    Yearly = 5
}

public enum DebtStatus
{
    Active = 1,
    Paid = 2
}

/// <summary>Sistema de cálculo de cuotas de un préstamo.</summary>
public enum AmortizationSystem
{
    /// <summary>Francés (préstamo de bancos): cuota total fija.</summary>
    French = 1,
    /// <summary>Alemán (préstamo ordinario): amortización de capital constante; cuota decreciente.</summary>
    German = 2,
    /// <summary>Interés simple: el interés se calcula siempre sobre el capital original.</summary>
    SimpleInterest = 3
}

public enum SavingsStatus
{
    Active = 1,
    Completed = 2
}

public static class PeriodTypeExtensions
{
    public static int PeriodsPerYear(this PeriodType period) => period switch
    {
        PeriodType.Weekly => 52,
        PeriodType.Biweekly => 26,
        PeriodType.Monthly => 12,
        PeriodType.Quarterly => 4,
        PeriodType.Yearly => 1,
        _ => throw new DomainException("Periodo no válido.")
    };

    public static DateOnly AddPeriods(this PeriodType period, DateOnly from, int count) => period switch
    {
        PeriodType.Weekly => from.AddDays(7 * count),
        PeriodType.Biweekly => from.AddDays(14 * count),
        PeriodType.Monthly => from.AddMonths(count),
        PeriodType.Quarterly => from.AddMonths(3 * count),
        PeriodType.Yearly => from.AddYears(count),
        _ => throw new DomainException("Periodo no válido.")
    };
}
