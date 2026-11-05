using ApiRestProject.Application.Ports.In;
using ApiRestProject.Application.Services;

namespace ApiRestProject.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<ITransactionService, TransactionService>();
        services.AddScoped<IFixedExpenseService, FixedExpenseService>();
        services.AddScoped<IDebtService, DebtService>();
        services.AddScoped<ISavingsService, SavingsService>();
        services.AddScoped<IBudgetService, BudgetService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IDashboardService, DashboardService>();
        return services;
    }
}
