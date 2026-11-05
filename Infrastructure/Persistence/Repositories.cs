using ApiRestProject.Application.Ports.Out;
using ApiRestProject.Domain;
using ApiRestProject.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApiRestProject.Infrastructure.Persistence;

/// <summary>Adaptadores de salida (EF Core + SQL Server) que implementan los puertos.</summary>
public abstract class EfRepository<T>(FinanzasDbContext db) : IRepository<T> where T : Entity
{
    protected FinanzasDbContext Db => db;

    protected virtual IQueryable<T> Query() => db.Set<T>();

    public virtual Task<T?> GetByIdAsync(int id, CancellationToken ct = default) =>
        Query().FirstOrDefaultAsync(e => e.Id == id, ct);

    public virtual Task<List<T>> ListAsync(CancellationToken ct = default) =>
        Query().OrderBy(e => e.Id).ToListAsync(ct);

    public async Task AddAsync(T entity, CancellationToken ct = default) => await db.Set<T>().AddAsync(entity, ct);

    public void Remove(T entity) => db.Set<T>().Remove(entity);

    public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}

public class CategoryRepository(FinanzasDbContext db) : EfRepository<Category>(db), ICategoryRepository
{
    public override Task<List<Category>> ListAsync(CancellationToken ct = default) =>
        Db.Categories.OrderBy(c => c.Type).ThenBy(c => c.Name).ToListAsync(ct);
}

public class TransactionRepository(FinanzasDbContext db) : EfRepository<Transaction>(db), ITransactionRepository
{
    protected override IQueryable<Transaction> Query() => Db.Transactions.Include(t => t.Category);

    public Task<List<Transaction>> SearchAsync(DateOnly? from, DateOnly? to, TransactionType? type, int? categoryId,
        CancellationToken ct = default)
    {
        var q = Query();
        if (from is not null) q = q.Where(t => t.Date >= from);
        if (to is not null) q = q.Where(t => t.Date <= to);
        if (type is not null) q = q.Where(t => t.Type == type);
        if (categoryId is not null) q = q.Where(t => t.CategoryId == categoryId);
        return q.OrderByDescending(t => t.Date).ThenByDescending(t => t.Id).ToListAsync(ct);
    }
}

public class FixedExpenseRepository(FinanzasDbContext db) : EfRepository<FixedExpense>(db), IFixedExpenseRepository
{
    protected override IQueryable<FixedExpense> Query() => Db.FixedExpenses.Include(f => f.Category);
}

public class UserRepository(FinanzasDbContext db) : EfRepository<User>(db), IUserRepository
{
    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        Db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

    public Task<bool> EmailExistsAsync(string email, int? excludingId, CancellationToken ct = default) =>
        Db.Users.AnyAsync(u => u.Email == email && u.Id != excludingId, ct);

    public Task<int> CountActiveAdminsAsync(int? excludingId, CancellationToken ct = default) =>
        Db.Users.CountAsync(u => u.Role == UserRole.Admin && u.IsActive && u.Id != excludingId, ct);
}

public class BudgetRepository(FinanzasDbContext db) : EfRepository<Budget>(db), IBudgetRepository
{
    protected override IQueryable<Budget> Query() => Db.Budgets.Include(b => b.Category);

    public Task<bool> ExistsForCategoryAsync(int categoryId, int? excludingId, CancellationToken ct = default) =>
        Db.Budgets.AnyAsync(b => b.CategoryId == categoryId && b.Id != excludingId, ct);
}

public class DebtRepository(FinanzasDbContext db) : EfRepository<Debt>(db), IDebtRepository
{
    protected override IQueryable<Debt> Query() => Db.Debts.Include(d => d.Payments);
}

public class SavingsPlanRepository(FinanzasDbContext db) : EfRepository<SavingsPlan>(db), ISavingsPlanRepository
{
    protected override IQueryable<SavingsPlan> Query() => Db.SavingsPlans.Include(s => s.Contributions);
}
