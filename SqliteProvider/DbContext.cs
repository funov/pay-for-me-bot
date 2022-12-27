using Microsoft.EntityFrameworkCore;
using SqliteProvider.Exceptions;
using SqliteProvider.Tables;

namespace SqliteProvider;

public sealed class DbContext : Microsoft.EntityFrameworkCore.DbContext
{
    private readonly string? connectionSting;

    public DbSet<UserTable> Users => Set<UserTable>();
    public DbSet<ProductTable> Products => Set<ProductTable>();
    public DbSet<UserProductBindingTable> UserProductBindings => Set<UserProductBindingTable>();
    public DbSet<BotPhrasesTable> BotPhrases => Set<BotPhrasesTable>();

    public DbContext(string? connectionSting)
    {
        this.connectionSting = connectionSting;
        Database.EnsureCreated();
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (connectionSting == null)
            throw new NullConnectionStringException("Configuration error");

        optionsBuilder.UseSqlite(connectionSting);
    }
}