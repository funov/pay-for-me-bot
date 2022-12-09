using Microsoft.EntityFrameworkCore;
using PayForMeBot.DbDriver.Exceptions;
using PayForMeBot.DbDriver.Models;


namespace PayForMeBot.DbDriver;

public sealed class DbContext : Microsoft.EntityFrameworkCore.DbContext
{
    private readonly string? connectionSting;

    public DbSet<UserTable> Users => Set<UserTable>();
    public DbSet<ProductTable> Products => Set<ProductTable>();
    public DbSet<UserProductTable> Bindings => Set<UserProductTable>();

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