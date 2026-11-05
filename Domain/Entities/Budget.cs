namespace ApiRestProject.Domain.Entities;

/// <summary>Presupuesto mensual recurrente para una categoría de gasto.</summary>
public class Budget : Entity, IOwned
{
    public int UserId { get; private set; }
    public int CategoryId { get; private set; }
    public Category? Category { get; private set; }
    public decimal MonthlyLimit { get; private set; }

    private Budget() { }

    public static Budget Create(Category category, decimal monthlyLimit)
    {
        var b = new Budget();
        b.Update(category, monthlyLimit);
        return b;
    }

    public void Update(Category category, decimal monthlyLimit)
    {
        if (category.Type != TransactionType.Expense)
            throw new DomainException("Solo se puede presupuestar una categoría de tipo gasto.");
        if (monthlyLimit <= 0) throw new DomainException("El límite mensual debe ser mayor a cero.");
        CategoryId = category.Id;
        Category = category;
        MonthlyLimit = monthlyLimit;
    }
}
