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
    public void AddUser(string userTgId, long teamId, string? spbLink)
    {
        var user = new UserTable { telegramId = userTgId, teamId = teamId, sbpLink = spbLink};
            
        db.Users?.Add(user);
        db.SaveChanges();
    }

    public long GetTeamIdByUserTgId(string userTgId)
    {
        var user = db.Users!.FirstOrDefault(s => s.telegramId!.Equals(userTgId));
        return user!.teamId;
    }
    
    public void AddSbpLink(string userTgId, Guid teamId, string? sbpLink)
    {
        var user = db.Users!.FirstOrDefault(s =>
            s.telegramId!.Equals(userTgId) && s.teamId.Equals(teamId));

        if (user != null) user.sbpLink = sbpLink;
        db.SaveChanges();
    }

    public double GetUserTotalPriceByTgId(string userTgId, Guid teamId)
    {
        var productsId = GetProductBindingsByUserTgId(userTgId, teamId)
            .Select(s => s.productId)
            .ToList();

        var productPrices = new List<double>();
        foreach (var productId in productsId)
        {
            productPrices.Add(GetProductByProductId(productId).Price);
        }
        
        return productPrices.Sum();
    }
    
    public UserProductTable[] GetProductBindingsByUserTgId(string userTgId, Guid teamId)
    {
        return db.Bindings!
            .Where(s =>
                s.userTelegramId!.Equals(userTgId) && s.teamId.Equals(teamId))
            .ToArray();
    }

    public Product GetProductByProductId(Guid id)
    {
        var product = db.Products!.FirstOrDefault(s => s.TeamId.Equals(id));

        return new Product 
            {Name = product!.Name, Price = product.Price, 
                TotalPrice = product.TotalPrice, Count = product.Count};
    }
    

    //Отжатие кнопочек
    public void DeleteUserProductBinding(string? userTelegramId, long teamId, Guid productId)
    {
        var binding = db.Bindings!.FirstOrDefault(s =>
            s.userTelegramId!.Equals(userTelegramId) && s.teamId.Equals(teamId));
        db.Bindings!.Remove(binding!);
        db.SaveChanges();
    }

    public string? GetSbpLinkByUserTgId(string userTgId)
    {
        var user = db.Users!.FirstOrDefault(s => s.telegramId!.Equals(userTgId));
        return user?.sbpLink;
    }

    // Когда прилетают кнопочки или ручной ввод
    public void AddProduct(Guid id, Product productModel, Guid receiptId, string buyerTelegramId, long teamId)
    {
        var productTable = new ProductTable { Id = id, Name = productModel.Name, TotalPrice = productModel.TotalPrice, 
            Price = productModel.Price, Count = productModel.Count, TeamId = teamId, 
            ReceiptId = receiptId, BuyerTelegramId = buyerTelegramId};
            
        db.Products?.Add(productTable);
        db.SaveChanges();
    }

    // Когда прилетают кнопочки или ручной ввод
    public void AddProducts(Guid[] ids, Product[] productModels, Guid receiptId, string buyerTelegramId, long teamId)
    {
        for (int i = 0; i < ids.Length; i++)
        {
            AddProduct(ids[i], productModels[i], receiptId, buyerTelegramId, teamId);
        }
    }

    // Нажатие кнопочек
    public void AddUserProductBinding(string? userTelegramId, long teamId, Guid productId)
    {
        var binding = new UserProductTable {userTelegramId = userTelegramId, productId = productId, teamId = teamId};
        db.Bindings!.Add(binding);
        db.SaveChanges();
    }
}