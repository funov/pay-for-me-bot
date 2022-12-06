using PayForMeBot.ReceiptApiClient.Models;
using PayForMeBot.SqliteDriver.Models;
using Product = PayForMeBot.ReceiptApiClient.Models.Product;

namespace PayForMeBot.SqliteDriver;

public class SqliteDriver : ISqliteDriver
{
    private readonly DbContext db;
    public SqliteDriver()
    {
        db = new DbContext();
    }
    
    public void AddUser(string telegramId, Guid teamId, string? spbLink)
    {
        var user = new UserTable { telegramId = telegramId, teamId = teamId, sbpLink = spbLink};
            
        db.Users.Add(user);
        db.SaveChanges();
    }

    public void AddSbpLink(string telegramId, Guid teamId, string? sbpLink)
    {
        var user = db.Users.FirstOrDefault(s =>
            s.telegramId.Equals(telegramId) && s.teamId.Equals(teamId));

        if (user != null) user.sbpLink = sbpLink;
        db.SaveChanges();
    }

    public void AddProduct(Product productModel, Guid receiptId, string telegramId, Guid teamId)
    {
        // id ??
        var product = new ProductTable { name = productModel.Name, totalPrice = productModel.TotalPrice, 
            teamId = teamId, receiptId = receiptId, buyerTelegramId = telegramId};
            
        db.Products.Add(product);
        db.SaveChanges();
    }

    public void AddProducts(Product[] products, Guid receiptId, string telegramId, Guid teamId)
    {
        foreach (var productModel in products)
        {
            AddProduct(productModel, receiptId, telegramId, teamId);
        }
    }

    public void AddUserProductBinding(string telegramId, Guid teamId, Guid productId)
    {
        // id ??
        var binding = new UserProductTable {userTelegramId = telegramId, productId = productId, teamId = teamId};
        db.Bindings.Add(binding);
        db.SaveChanges();
    }
}