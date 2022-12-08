using Microsoft.EntityFrameworkCore;
using PayForMeBot.SqliteDriver.Models;
using Product = PayForMeBot.SqliteDriver.Models;

namespace PayForMeBot.SqliteDriver;

public sealed class DbContext : Microsoft.EntityFrameworkCore.DbContext
{
    public DbSet<UserTable>? Users { get; set; }
    public DbSet<ProductTable>? Products { get; set; }    
    public DbSet<UserProductTable>? Bindings { get; set; }

    public DbContext()
    {
        Database.EnsureCreated();
    }
 
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite(@"Data Source=UsersProducts.db");
    }
}