using System.ComponentModel.DataAnnotations;
using ApiRestProject.Domain;
using ApiRestProject.Domain.Entities;

namespace ApiRestProject.Application.Dtos;

public class NotFoundException(string message) : Exception(message);

/// <summary>Credenciales inválidas o sesión no autorizada (se traduce a HTTP 401).</summary>
public class UnauthorizedException(string message) : Exception(message);

// ---------- Autenticación y usuarios ----------
public record LoginRequest([Required, MaxLength(254)] string Email, [Required, MaxLength(100)] string Password);

public record RegisterRequest(
    [Required, MaxLength(100)] string Name,
    [Required, MaxLength(254)] string Email,
    [Required, MaxLength(100)] string Password);

public record ChangePasswordRequest(
    [Required, MaxLength(100)] string CurrentPassword,
    [Required, MaxLength(100)] string NewPassword);

public record UserDto(int Id, string Name, string Email, UserRole Role, bool IsActive, DateTime CreatedAt);

public record AuthResponse(string Token, DateTime ExpiresAt, UserDto User);

public record UserCreateRequest(
    [Required, MaxLength(100)] string Name,
    [Required, MaxLength(254)] string Email,
    [Required, MaxLength(100)] string Password,
    UserRole Role);

public record UserUpdateRequest([Required, MaxLength(100)] string Name, UserRole Role, bool IsActive);

public record ResetPasswordRequest([Required, MaxLength(100)] string NewPassword);

// ---------- Categorías ----------
public record CategoryRequest([Required, MaxLength(100)] string Name, TransactionType Type);
public record CategoryDto(int Id, string Name, TransactionType Type);

// ---------- Transacciones ----------
public record TransactionRequest(
    [Range(1, int.MaxValue)] int CategoryId,
    [Range(0.01, 999999999999)] decimal Amount,
    DateOnly Date,
    [MaxLength(300)] string? Description);

public record TransactionDto(int Id, int CategoryId, string CategoryName, TransactionType Type,
    decimal Amount, DateOnly Date, string? Description);

// ---------- Gastos fijos ----------
public record FixedExpenseRequest(
    [Required, MaxLength(150)] string Name,
    [Range(0.01, 999999999999)] decimal Amount,
    [Range(1, 31)] int DayOfMonth,
    int? CategoryId,
    bool IsActive = true);

public record FixedExpenseDto(int Id, string Name, decimal Amount, int DayOfMonth,
    int? CategoryId, string? CategoryName, bool IsActive);

// ---------- Deudas ----------
public record DebtRequest(
    [Required, MaxLength(150)] string Name,
    [MaxLength(150)] string? Creditor,
    [Range(0.01, 999999999999)] decimal Principal,
    [Range(0, 1000)] decimal InterestRatePerPeriod,
    PeriodType Period,
    [Range(1, 1200)] int TotalPeriods,
    DateOnly StartDate,
    AmortizationSystem AmortizationSystem = AmortizationSystem.French);

public record DebtUpdateRequest([Required, MaxLength(150)] string Name, [MaxLength(150)] string? Creditor);

public record DebtDto(int Id, string Name, string? Creditor, decimal Principal, decimal InterestRatePerPeriod,
    PeriodType Period, int TotalPeriods, DateOnly StartDate, decimal Balance, DebtStatus Status,
    AmortizationSystem AmortizationSystem,
    decimal CurrentInstallment, decimal MonthlyCommitment, int PaymentsMade, int RemainingPeriods, decimal TotalInterestPaid);

public record DebtPaymentRequest([Range(0.01, 999999999999)] decimal Amount, DateOnly Date);

public record DebtPaymentDto(int Id, DateOnly Date, decimal Amount, decimal Interest, decimal Capital, decimal BalanceAfter);

public record ScheduleRowDto(int Number, DateOnly DueDate, decimal Installment, decimal Interest, decimal Capital, decimal Balance);

// ---------- Ahorro ----------
public record SavingsPlanRequest(
    [Required, MaxLength(150)] string Name,
    [Range(0.01, 999999999999)] decimal TargetAmount,
    DateOnly StartDate,
    DateOnly TargetDate);

public record SavingsPlanDto(int Id, string Name, decimal TargetAmount, decimal CurrentAmount, decimal ProgressPercent,
    DateOnly StartDate, DateOnly TargetDate, SavingsStatus Status, decimal SuggestedMonthlyContribution);

public record ContributionRequest([Range(0.01, 999999999999)] decimal Amount, DateOnly Date, [MaxLength(300)] string? Note);
public record ContributionDto(int Id, DateOnly Date, decimal Amount, string? Note);

// ---------- Dashboard ----------
public record CategoryTotalDto(string Category, decimal Total);

public record DashboardDto(
    int Year, int Month,
    decimal Income,
    decimal FixedExpenses,
    decimal VariableExpenses,
    decimal DebtPayments,
    decimal SavingsContributions,
    decimal TotalOutflow,
    decimal Available,
    List<CategoryTotalDto> ExpensesByCategory);

// ---------- Presupuestos ----------
public record BudgetRequest(
    [Range(1, int.MaxValue)] int CategoryId,
    [Range(0.01, 999999999999)] decimal MonthlyLimit);

public enum BudgetState
{
    Ok = 1,
    Warning = 2,   // >= 80% del límite
    Exceeded = 3   // > 100% del límite
}

public record BudgetDto(int Id, int CategoryId, string CategoryName, decimal MonthlyLimit);

public record BudgetStatusDto(int Id, int CategoryId, string CategoryName, decimal MonthlyLimit,
    decimal Spent, decimal Remaining, decimal PercentUsed, BudgetState State);

// ---------- Reportes ----------
public record MonthlyTrendDto(int Year, int Month, decimal Income, decimal FixedExpenses,
    decimal VariableExpenses, decimal TotalExpenses, decimal Net);

public record CategoryBreakdownDto(string Category, decimal Total, decimal Percent);

// ---------- Mapeos ----------
public static class Mappers
{
    public static CategoryDto ToDto(this Category c) => new(c.Id, c.Name, c.Type);

    public static UserDto ToDto(this User u) => new(u.Id, u.Name, u.Email, u.Role, u.IsActive, u.CreatedAt);

    public static TransactionDto ToDto(this Transaction t) =>
        new(t.Id, t.CategoryId, t.Category?.Name ?? string.Empty, t.Type, t.Amount, t.Date, t.Description);

    public static BudgetDto ToDto(this Budget b) =>
        new(b.Id, b.CategoryId, b.Category?.Name ?? string.Empty, b.MonthlyLimit);

    public static FixedExpenseDto ToDto(this FixedExpense f) =>
        new(f.Id, f.Name, f.Amount, f.DayOfMonth, f.CategoryId, f.Category?.Name, f.IsActive);

    public static DebtDto ToDto(this Debt d) =>
        new(d.Id, d.Name, d.Creditor, d.Principal, d.InterestRatePerPeriod, d.Period, d.TotalPeriods, d.StartDate,
            d.Balance, d.Status, d.AmortizationSystem, d.CurrentInstallment, d.MonthlyCommitment, d.Payments.Count,
            d.RemainingPeriods, d.TotalInterestPaid);

    public static DebtPaymentDto ToDto(this DebtPayment p) =>
        new(p.Id, p.Date, p.Amount, p.Interest, p.Capital, p.BalanceAfter);

    public static ScheduleRowDto ToDto(this ScheduleRow r) =>
        new(r.Number, r.DueDate, r.Installment, r.Interest, r.Capital, r.Balance);

    public static SavingsPlanDto ToDto(this SavingsPlan s, DateOnly today) =>
        new(s.Id, s.Name, s.TargetAmount, s.CurrentAmount, s.ProgressPercent, s.StartDate, s.TargetDate,
            s.Status, s.SuggestedMonthlyContribution(today));

    public static ContributionDto ToDto(this SavingsContribution c) => new(c.Id, c.Date, c.Amount, c.Note);
}
