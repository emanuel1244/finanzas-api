using ApiRestProject.Application.Ports.Out;
using ApiRestProject.Domain;
using ApiRestProject.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApiRestProject.Infrastructure.Persistence;

public class FinanzasDbContext(DbContextOptions<FinanzasDbContext> options, ICurrentUser currentUser) : DbContext(options)
{
    /// <summary>Usuario de la petición actual; los filtros globales lo usan para aislar los datos.</summary>
    private int CurrentUserId => currentUser.UserId;

    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<FixedExpense> FixedExpenses => Set<FixedExpense>();
    public DbSet<Budget> Budgets => Set<Budget>();
    public DbSet<Debt> Debts => Set<Debt>();
    public DbSet<DebtPayment> DebtPayments => Set<DebtPayment>();
    public DbSet<SavingsPlan> SavingsPlans => Set<SavingsPlan>();
    public DbSet<SavingsContribution> SavingsContributions => Set<SavingsContribution>();

    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        builder.Properties<decimal>().HavePrecision(18, 2);
    }

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>(e =>
        {
            e.ToTable("Users");
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.Email).HasMaxLength(254).IsRequired();
            e.Property(x => x.PasswordHash).HasMaxLength(300).IsRequired();
            e.HasIndex(x => x.Email).IsUnique();
        });

        b.Entity<Category>(e =>
        {
            e.ToTable("Categories");
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.HasIndex(x => new { x.Name, x.Type }).IsUnique();
            e.HasData(
                new { Id = 1, Name = "Salario", Type = TransactionType.Income },
                new { Id = 2, Name = "Ingresos extra", Type = TransactionType.Income },
                new { Id = 3, Name = "Alimentación", Type = TransactionType.Expense },
                new { Id = 4, Name = "Transporte", Type = TransactionType.Expense },
                new { Id = 5, Name = "Vivienda", Type = TransactionType.Expense },
                new { Id = 6, Name = "Servicios", Type = TransactionType.Expense },
                new { Id = 7, Name = "Salud", Type = TransactionType.Expense },
                new { Id = 8, Name = "Entretenimiento", Type = TransactionType.Expense },
                new { Id = 9, Name = "Educación", Type = TransactionType.Expense },
                new { Id = 10, Name = "Otros gastos", Type = TransactionType.Expense });
        });

        b.Entity<Transaction>(e =>
        {
            e.ToTable("Transactions", t => t.HasCheckConstraint("CK_Transactions_Amount", "[Amount] > 0"));
            e.Property(x => x.Description).HasMaxLength(300);
            e.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => x.Date);
        });

        b.Entity<FixedExpense>(e =>
        {
            e.ToTable("FixedExpenses", t => t.HasCheckConstraint("CK_FixedExpenses_Day", "[DayOfMonth] BETWEEN 1 AND 31"));
            e.Property(x => x.Name).HasMaxLength(150).IsRequired();
            e.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.SetNull);
        });

        b.Entity<Budget>(e =>
        {
            e.ToTable("Budgets", t => t.HasCheckConstraint("CK_Budgets_Limit", "[MonthlyLimit] > 0"));
            e.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.UserId, x.CategoryId }).IsUnique();
        });

        b.Entity<Debt>(e =>
        {
            e.ToTable("Debts");
            e.Property(x => x.Name).HasMaxLength(150).IsRequired();
            e.Property(x => x.Creditor).HasMaxLength(150);
            e.Property(x => x.InterestRatePerPeriod).HasPrecision(9, 4);
            e.Property(x => x.AmortizationSystem).HasConversion<int>();
            e.HasMany(x => x.Payments).WithOne().HasForeignKey(p => p.DebtId).OnDelete(DeleteBehavior.Cascade);
            e.Navigation(x => x.Payments).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        b.Entity<DebtPayment>(e =>
        {
            e.ToTable("DebtPayments");
            e.HasIndex(x => new { x.DebtId, x.Date });
        });

        b.Entity<SavingsPlan>(e =>
        {
            e.ToTable("SavingsPlans");
            e.Property(x => x.Name).HasMaxLength(150).IsRequired();
            e.HasMany(x => x.Contributions).WithOne().HasForeignKey(c => c.SavingsPlanId).OnDelete(DeleteBehavior.Cascade);
            e.Navigation(x => x.Contributions).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        b.Entity<SavingsContribution>(e =>
        {
            e.ToTable("SavingsContributions");
            e.Property(x => x.Note).HasMaxLength(300);
        });

        // Datos propios de cada usuario: filtro global + clave foránea a Users.
        ConfigureOwned<Transaction>(b);
        ConfigureOwned<FixedExpense>(b);
        ConfigureOwned<Budget>(b);
        ConfigureOwned<Debt>(b);
        ConfigureOwned<SavingsPlan>(b);
    }

    private void ConfigureOwned<T>(ModelBuilder b) where T : class, IOwned
    {
        var entity = b.Entity<T>();
        entity.HasQueryFilter(x => EF.Property<int>(x, nameof(IOwned.UserId)) == CurrentUserId);
        entity.HasOne<User>().WithMany().HasForeignKey(nameof(IOwned.UserId)).OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(nameof(IOwned.UserId));
    }

    /// <summary>Asigna automáticamente el dueño (usuario actual) a los registros nuevos.</summary>
    private void StampOwner()
    {
        foreach (var entry in ChangeTracker.Entries<IOwned>().Where(e => e.State == EntityState.Added))
        {
            var prop = entry.Property(nameof(IOwned.UserId));
            if ((int)prop.CurrentValue! == 0) prop.CurrentValue = CurrentUserId;
        }
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        StampOwner();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        StampOwner();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }
}
