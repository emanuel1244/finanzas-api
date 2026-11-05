namespace ApiRestProject.Domain.Entities;

/// <summary>Ingreso o gasto variable (del día a día).</summary>
public class Transaction : Entity, IOwned
{
    public int UserId { get; private set; }
    public int CategoryId { get; private set; }
    public Category? Category { get; private set; }
    public TransactionType Type { get; private set; }
    public decimal Amount { get; private set; }
    public DateOnly Date { get; private set; }
    public string? Description { get; private set; }

    private Transaction() { }

    public static Transaction Create(Category category, decimal amount, DateOnly date, string? description)
    {
        var t = new Transaction();
        t.Update(category, amount, date, description);
        return t;
    }

    public void Update(Category category, decimal amount, DateOnly date, string? description)
    {
        if (amount <= 0) throw new DomainException("El monto debe ser mayor a cero.");
        CategoryId = category.Id;
        Category = category;
        Type = category.Type;
        Amount = amount;
        Date = date;
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }
}
