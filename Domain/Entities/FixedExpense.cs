namespace ApiRestProject.Domain.Entities;

/// <summary>Gasto fijo mensual (renta, internet, suscripciones...).</summary>
public class FixedExpense : Entity, IOwned
{
    public int UserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public int DayOfMonth { get; private set; }
    public int? CategoryId { get; private set; }
    public Category? Category { get; private set; }
    public bool IsActive { get; private set; } = true;

    private FixedExpense() { }

    public static FixedExpense Create(string name, decimal amount, int dayOfMonth, int? categoryId, bool isActive)
    {
        var f = new FixedExpense();
        f.Update(name, amount, dayOfMonth, categoryId, isActive);
        return f;
    }

    public void Update(string name, decimal amount, int dayOfMonth, int? categoryId, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("El nombre es obligatorio.");
        if (amount <= 0) throw new DomainException("El monto debe ser mayor a cero.");
        if (dayOfMonth is < 1 or > 31) throw new DomainException("El día del mes debe estar entre 1 y 31.");
        Name = name.Trim();
        Amount = amount;
        DayOfMonth = dayOfMonth;
        CategoryId = categoryId;
        IsActive = isActive;
    }
}
