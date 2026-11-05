namespace ApiRestProject.Domain.Entities;

public class Category : Entity
{
    public string Name { get; private set; } = string.Empty;
    public TransactionType Type { get; private set; }

    private Category() { }

    public static Category Create(string name, TransactionType type)
    {
        var c = new Category();
        c.Update(name, type);
        return c;
    }

    public void Update(string name, TransactionType type)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("El nombre de la categoría es obligatorio.");
        Name = name.Trim();
        Type = type;
    }
}
