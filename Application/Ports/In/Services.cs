using ApiRestProject.Application.Dtos;
using ApiRestProject.Domain;

namespace ApiRestProject.Application.Ports.In;

/// <summary>Puertos de entrada: casos de uso que expone la aplicación.</summary>
public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<UserDto> GetCurrentAsync(CancellationToken ct = default);
    Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct = default);
}

public interface IUserService
{
    Task<List<UserDto>> ListAsync(CancellationToken ct = default);
    Task<UserDto> CreateAsync(UserCreateRequest request, CancellationToken ct = default);
    Task<UserDto> UpdateAsync(int id, UserUpdateRequest request, CancellationToken ct = default);
    Task ResetPasswordAsync(int id, ResetPasswordRequest request, CancellationToken ct = default);
}

public interface ICategoryService
{
    Task<List<CategoryDto>> ListAsync(CancellationToken ct = default);
    Task<CategoryDto> CreateAsync(CategoryRequest request, CancellationToken ct = default);
    Task<CategoryDto> UpdateAsync(int id, CategoryRequest request, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}

public interface ITransactionService
{
    Task<List<TransactionDto>> SearchAsync(DateOnly? from, DateOnly? to, TransactionType? type, int? categoryId, CancellationToken ct = default);
    Task<TransactionDto> GetAsync(int id, CancellationToken ct = default);
    Task<TransactionDto> CreateAsync(TransactionRequest request, CancellationToken ct = default);
    Task<TransactionDto> UpdateAsync(int id, TransactionRequest request, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}

public interface IFixedExpenseService
{
    Task<List<FixedExpenseDto>> ListAsync(CancellationToken ct = default);
    Task<FixedExpenseDto> CreateAsync(FixedExpenseRequest request, CancellationToken ct = default);
    Task<FixedExpenseDto> UpdateAsync(int id, FixedExpenseRequest request, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}

public interface IDebtService
{
    Task<List<DebtDto>> ListAsync(CancellationToken ct = default);
    Task<DebtDto> GetAsync(int id, CancellationToken ct = default);
    Task<DebtDto> CreateAsync(DebtRequest request, CancellationToken ct = default);
    Task<DebtDto> UpdateAsync(int id, DebtUpdateRequest request, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
    Task<List<ScheduleRowDto>> GetScheduleAsync(int id, CancellationToken ct = default);
    Task<List<DebtPaymentDto>> GetPaymentsAsync(int id, CancellationToken ct = default);
    Task<DebtPaymentDto> RegisterPaymentAsync(int id, DebtPaymentRequest request, CancellationToken ct = default);
}

public interface ISavingsService
{
    Task<List<SavingsPlanDto>> ListAsync(CancellationToken ct = default);
    Task<SavingsPlanDto> GetAsync(int id, CancellationToken ct = default);
    Task<SavingsPlanDto> CreateAsync(SavingsPlanRequest request, CancellationToken ct = default);
    Task<SavingsPlanDto> UpdateAsync(int id, SavingsPlanRequest request, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
    Task<List<ContributionDto>> GetContributionsAsync(int id, CancellationToken ct = default);
    Task<ContributionDto> AddContributionAsync(int id, ContributionRequest request, CancellationToken ct = default);
}

public interface IBudgetService
{
    Task<List<BudgetDto>> ListAsync(CancellationToken ct = default);
    Task<List<BudgetStatusDto>> GetStatusAsync(int year, int month, CancellationToken ct = default);
    Task<BudgetDto> CreateAsync(BudgetRequest request, CancellationToken ct = default);
    Task<BudgetDto> UpdateAsync(int id, BudgetRequest request, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}

public interface IReportService
{
    Task<List<MonthlyTrendDto>> GetMonthlyTrendAsync(int months, DateOnly today, CancellationToken ct = default);
    Task<List<CategoryBreakdownDto>> GetExpensesByCategoryAsync(DateOnly from, DateOnly to, CancellationToken ct = default);
}

public interface IDashboardService
{
    Task<DashboardDto> GetMonthlySummaryAsync(int year, int month, CancellationToken ct = default);
}
