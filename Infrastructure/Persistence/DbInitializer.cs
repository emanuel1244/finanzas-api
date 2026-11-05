using ApiRestProject.Application.Ports.Out;
using ApiRestProject.Domain;
using ApiRestProject.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApiRestProject.Infrastructure.Persistence;

/// <summary>
/// Prepara la base de datos al arrancar. Como el proyecto usa EnsureCreated (sin migraciones),
/// aquí se actualiza de forma idempotente una base ya existente: crea la tabla Users,
/// agrega la columna UserId a los datos y asigna los registros previos al administrador inicial.
/// </summary>
public static class DbInitializer
{
    private static readonly string[] OwnedTables = ["Transactions", "FixedExpenses", "Budgets", "Debts", "SavingsPlans"];

    public static async Task InitializeAsync(FinanzasDbContext db, IPasswordHasher hasher, IConfiguration config, ILogger logger)
    {
        await db.Database.EnsureCreatedAsync();

        await db.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'[Users]', N'U') IS NULL
            BEGIN
                CREATE TABLE [Users] (
                    [Id] int NOT NULL IDENTITY,
                    [Name] nvarchar(100) NOT NULL,
                    [Email] nvarchar(254) NOT NULL,
                    [PasswordHash] nvarchar(300) NOT NULL,
                    [Role] int NOT NULL,
                    [IsActive] bit NOT NULL,
                    [CreatedAt] datetime2 NOT NULL,
                    CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
                );
                CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]);
            END
            """);

        await db.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'[Budgets]', N'U') IS NULL
            BEGIN
                CREATE TABLE [Budgets] (
                    [Id] int NOT NULL IDENTITY,
                    [UserId] int NOT NULL,
                    [CategoryId] int NOT NULL,
                    [MonthlyLimit] decimal(18,2) NOT NULL,
                    CONSTRAINT [PK_Budgets] PRIMARY KEY ([Id]),
                    CONSTRAINT [CK_Budgets_Limit] CHECK ([MonthlyLimit] > 0),
                    CONSTRAINT [FK_Budgets_Categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([Id]) ON DELETE CASCADE,
                    CONSTRAINT [FK_Budgets_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id])
                );
                CREATE INDEX [IX_Budgets_UserId] ON [Budgets] ([UserId]);
            END
            """);

        var adminId = await EnsureAdminAsync(db, hasher, config, logger);

        // Tablas que existían antes de los usuarios: agrega UserId y asigna lo existente al administrador.
#pragma warning disable EF1002 // Los nombres de tabla son constantes y adminId es un entero.
        foreach (var t in OwnedTables)
        {
            await db.Database.ExecuteSqlRawAsync($"""
                IF COL_LENGTH(N'{t}', N'UserId') IS NULL
                BEGIN
                    ALTER TABLE [{t}] ADD [UserId] int NULL;
                    EXEC(N'UPDATE [{t}] SET [UserId] = {adminId} WHERE [UserId] IS NULL');
                    EXEC(N'ALTER TABLE [{t}] ALTER COLUMN [UserId] int NOT NULL');
                    EXEC(N'ALTER TABLE [{t}] ADD CONSTRAINT [FK_{t}_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id])');
                    EXEC(N'CREATE INDEX [IX_{t}_UserId] ON [{t}] ([UserId])');
                END
                """);
        }
#pragma warning restore EF1002

        // Un presupuesto por categoría y por usuario (antes era uno por categoría en total).
        await db.Database.ExecuteSqlRawAsync("""
            IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Budgets_CategoryId' AND object_id = OBJECT_ID(N'[Budgets]'))
                DROP INDEX [IX_Budgets_CategoryId] ON [Budgets];
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Budgets_UserId_CategoryId' AND object_id = OBJECT_ID(N'[Budgets]'))
                CREATE UNIQUE INDEX [IX_Budgets_UserId_CategoryId] ON [Budgets] ([UserId], [CategoryId]);
            """);

        // Sistema de amortización de las deudas (francés por defecto para las ya existentes).
        await db.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'[Debts]', N'U') IS NOT NULL AND COL_LENGTH(N'Debts', N'AmortizationSystem') IS NULL
            BEGIN
                ALTER TABLE [Debts] ADD [AmortizationSystem] int NOT NULL CONSTRAINT [DF_Debts_AmortizationSystem] DEFAULT 1;
            END
            """);
    }

    /// <summary>Devuelve el Id de un administrador; si no hay usuarios, crea el inicial desde la sección "Seed".</summary>
    private static async Task<int> EnsureAdminAsync(FinanzasDbContext db, IPasswordHasher hasher, IConfiguration config, ILogger logger)
    {
        var existing = await db.Users.Where(u => u.Role == UserRole.Admin).OrderBy(u => u.Id).Select(u => (int?)u.Id).FirstOrDefaultAsync();
        if (existing is not null) return existing.Value;

        var name = config["Seed:AdminName"] ?? "Administrador";
        var email = config["Seed:AdminEmail"] ?? "admin@finanzas.local";
        var password = config["Seed:AdminPassword"];
        if (string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("Falta 'Seed:AdminPassword' para crear el administrador inicial.");

        var admin = User.Create(name, email, hasher.Hash(password), UserRole.Admin);
        db.Users.Add(admin);
        await db.SaveChangesAsync();

        logger.LogWarning("Se creó el administrador inicial '{Email}'. Cambia su contraseña desde la aplicación.", admin.Email);
        return admin.Id;
    }
}
