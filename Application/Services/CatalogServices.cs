using ApiRestProject.Application.Dtos;
using ApiRestProject.Application.Ports.In;
using ApiRestProject.Application.Ports.Out;
using ApiRestProject.Domain;
using ApiRestProject.Domain.Entities;

namespace ApiRestProject.Application.Services;

public class CategoryService(ICategoryRepository repo) : ICategoryService
{
    public async Task<List<CategoryDto>> ListAsync(CancellationToken ct = default) =>
        (await repo.ListAsync(ct)).Select(c => c.ToDto()).ToList();

    public async Task<CategoryDto> CreateAsync(CategoryRequest r, CancellationToken ct = default)
    {
        var c = Category.Create(r.Name, r.Type);
        await repo.AddAsync(c, ct);
        await repo.SaveChangesAsync(ct);
        return c.ToDto();
    }

    public async Task<CategoryDto> UpdateAsync(int id, CategoryRequest r, CancellationToken ct = default)
    {
        var c = await repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Categoría no encontrada.");
        c.Update(r.Name, r.Type);
        await repo.SaveChangesAsync(ct);
        return c.ToDto();
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var c = await repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Categoría no encontrada.");
        repo.Remove(c);
        await repo.SaveChangesAsync(ct);
    }
}

public class TransactionService(ITransactionRepository repo, ICategoryRepository categories) : ITransactionService
{
    public async Task<List<TransactionDto>> SearchAsync(DateOnly? from, DateOnly? to, TransactionType? type,
        int? categoryId, CancellationToken ct = default) =>
        (await repo.SearchAsync(from, to, type, categoryId, ct)).Select(t => t.ToDto()).ToList();

    public async Task<TransactionDto> GetAsync(int id, CancellationToken ct = default) =>
        (await repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Transacción no encontrada.")).ToDto();

    public async Task<TransactionDto> CreateAsync(TransactionRequest r, CancellationToken ct = default)
    {
        var category = await categories.GetByIdAsync(r.CategoryId, ct)
            ?? throw new DomainException("La categoría indicada no existe.");
        var t = Transaction.Create(category, r.Amount, r.Date, r.Description);
        await repo.AddAsync(t, ct);
        await repo.SaveChangesAsync(ct);
        return t.ToDto();
    }

    public async Task<TransactionDto> UpdateAsync(int id, TransactionRequest r, CancellationToken ct = default)
    {
        var t = await repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Transacción no encontrada.");
        var category = await categories.GetByIdAsync(r.CategoryId, ct)
            ?? throw new DomainException("La categoría indicada no existe.");
        t.Update(category, r.Amount, r.Date, r.Description);
        await repo.SaveChangesAsync(ct);
        return t.ToDto();
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var t = await repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Transacción no encontrada.");
        repo.Remove(t);
        await repo.SaveChangesAsync(ct);
    }
}

public class FixedExpenseService(IFixedExpenseRepository repo, ICategoryRepository categories) : IFixedExpenseService
{
    public async Task<List<FixedExpenseDto>> ListAsync(CancellationToken ct = default) =>
        (await repo.ListAsync(ct)).Select(f => f.ToDto()).ToList();

    public async Task<FixedExpenseDto> CreateAsync(FixedExpenseRequest r, CancellationToken ct = default)
    {
        await EnsureCategoryAsync(r.CategoryId, ct);
        var f = FixedExpense.Create(r.Name, r.Amount, r.DayOfMonth, r.CategoryId, r.IsActive);
        await repo.AddAsync(f, ct);
        await repo.SaveChangesAsync(ct);
        return (await repo.GetByIdAsync(f.Id, ct))!.ToDto();
    }

    public async Task<FixedExpenseDto> UpdateAsync(int id, FixedExpenseRequest r, CancellationToken ct = default)
    {
        var f = await repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Gasto fijo no encontrado.");
        await EnsureCategoryAsync(r.CategoryId, ct);
        f.Update(r.Name, r.Amount, r.DayOfMonth, r.CategoryId, r.IsActive);
        await repo.SaveChangesAsync(ct);
        return (await repo.GetByIdAsync(id, ct))!.ToDto();
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var f = await repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Gasto fijo no encontrado.");
        repo.Remove(f);
        await repo.SaveChangesAsync(ct);
    }

    private async Task EnsureCategoryAsync(int? categoryId, CancellationToken ct)
    {
        if (categoryId is null) return;
        var c = await categories.GetByIdAsync(categoryId.Value, ct)
            ?? throw new DomainException("La categoría indicada no existe.");
        if (c.Type != TransactionType.Expense)
            throw new DomainException("Un gasto fijo debe usar una categoría de tipo gasto.");
    }
}
