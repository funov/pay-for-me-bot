using Microsoft.EntityFrameworkCore;
using PayForMeBot.SqliteDriver.Models;
using Product = PayForMeBot.SqliteDriver.Models;

namespace PayForMeBot.SqliteDriver;

public sealed class DbContext : Microsoft.EntityFrameworkCore.DbContext
{
    public DbSet<UserTable>? Users { get; set; }
    public DbSet<ProductTable>? Products { get; set; }    
    public DbSet<UserProductTable>? Bindings { get; set; }
    
    public string DbPath { get; }

    public DbContext()
    {
        var folder = Environment.CurrentDirectory;
        // Environment.SpecialFolder.LocalApplicationData;
        // var path = Environment.GetFolderPath();
        DbPath = Path.Join(folder, "UsersAndProducts.db");
        // Database.EnsureCreated();
    }
 
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite($"Data Source={DbPath}");
        // optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=UsersAndProducts;Trusted_Connection=True;");
    }
}