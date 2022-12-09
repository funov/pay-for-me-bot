using Microsoft.EntityFrameworkCore;
using PayForMeBot.SqliteDriver.Models;


namespace PayForMeBot.SqliteDriver;

public sealed class DbContext : Microsoft.EntityFrameworkCore.DbContext
{
    private readonly string connectionSting;
    
    public DbSet<UserTable> Users => Set<UserTable>();
    public DbSet<ProductTable> Products => Set<ProductTable>();
    public DbSet<UserProductTable> Bindings => Set<UserProductTable>();

    public DbContext(string connectionSting)
    {
        this.connectionSting = connectionSting;
        Database.EnsureCreated();
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite(connectionSting);
    }
}