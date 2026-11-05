using ApiRestProject.Application.Ports.Out;
using ApiRestProject.Infrastructure.Persistence;
using ApiRestProject.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace ApiRestProject.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var cs = config.GetConnectionString("FinanzasDb");
        if (string.IsNullOrWhiteSpace(cs))
            throw new InvalidOperationException(
                "Falta la cadena de conexión 'FinanzasDb'. " +
                $"Entorno actual: '{System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "(no definido)"}'. " +
                "En local usa ASPNETCORE_ENVIRONMENT=dev para cargar appsettings.dev.json. " +
                "En EC2 (stage) define la variable de entorno ConnectionStrings__FinanzasDb.");

        services.AddDbContext<FinanzasDbContext>(o => o.UseSqlServer(cs));
        services.Configure<JwtOptions>(config.GetSection("Jwt"));
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<IFixedExpenseRepository, FixedExpenseRepository>();
        services.AddScoped<IBudgetRepository, BudgetRepository>();
        services.AddScoped<IDebtRepository, DebtRepository>();
        services.AddScoped<ISavingsPlanRepository, SavingsPlanRepository>();
        return services;
    }
}
