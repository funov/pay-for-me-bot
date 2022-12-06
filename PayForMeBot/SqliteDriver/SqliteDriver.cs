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
    
    // Создать команду, Присоединиться к команде
    public void AddUser(string userTgId, Guid teamId, string? spbLink)
    {
        var user = new UserTable { telegramId = userTgId, teamId = teamId, sbpLink = spbLink};
            
        db.Users.Add(user);
        db.SaveChanges();
    }
    
    public void AddSbpLink(string userTgId, Guid teamId, string? sbpLink)
    {
        var user = db.Users.FirstOrDefault(s =>
            s.telegramId.Equals(userTgId) && s.teamId.Equals(teamId));

        if (user != null) user.sbpLink = sbpLink;
        db.SaveChanges();
    }
    
    public string GetUserTotalPricesByTgId(string userTgId)
    {
        // GetUserProductBindingById -> GetProductById -> Price
        throw new NotImplementedException();
    }
    
    public string GetUserProductBindingByUserTgId(string userTgId, Guid teamId)
    {
        throw new NotImplementedException();
    }

    public string GetProductById(Guid id, Guid teamId)
    {
        throw new NotImplementedException();
    }

    public string GetSbpLinkByUserTgId(string tgId)
    {
        throw new NotImplementedException();
    }

    // Когда прилетают кнопочки или ручной ввод
    public void AddProduct(Guid id, Product productModel, Guid receiptId, string buyerTelegramId, Guid teamId)
    {
        // id ??
        var product = new ProductTable { name = productModel.Name, totalPrice = productModel.TotalPrice, 
            teamId = teamId, receiptId = receiptId, buyerTelegramId = buyerTelegramId};
            
        db.Products.Add(product);
        db.SaveChanges();
    }

    // Когда прилетают кнопочки или ручной ввод
    public void AddProducts(Guid[] id, Product[] productModels, Guid receiptId, string buyerTelegramId, Guid teamId)
    {
        foreach (var productModel in productModels)
        {
            AddProduct(productModel, receiptId, buyerTelegramId, teamId);
        }
    }

    // Нажатие кнопочек
    public void AddUserProductBinding(string userTelegramId, Guid teamId, Guid productId)
    {
        // id автоматически
        var binding = new UserProductTable {userTelegramId = userTelegramId, productId = productId, teamId = teamId};
        db.Bindings.Add(binding);
        db.SaveChanges();
    }
}