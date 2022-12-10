using Microsoft.Extensions.Configuration;
using PayForMeBot.DbDriver.Models;
using Product = PayForMeBot.ReceiptApiClient.Models.Product;

namespace PayForMeBot.DbDriver;

public class DbDriver : IDbDriver
{
    private readonly DbContext db;

    public DbDriver(IConfiguration config) => db = new DbContext(config.GetValue<string>("DbConnectionString"));

    public void AddUser(string userTgId, long teamId, string? spbLink)
    {
        var user = new UserTable { UserTelegramId = userTgId, TeamId = teamId, SbpLink = spbLink };

        db.Users.Add(user);
        db.SaveChanges();
    }

    public long GetTeamIdByUserTgId(string userTgId)
    {
        var user = db.Users.FirstOrDefault(userTable => userTable.UserTelegramId!.Equals(userTgId));
        return user!.TeamId;
    }

    public void AddSbpLink(string userTgId, Guid teamId, string? sbpLink)
    {
        var user = db.Users.FirstOrDefault(userTable
            => userTable.UserTelegramId!.Equals(userTgId) && userTable.TeamId.Equals(teamId));

        if (user != null)
            user.SbpLink = sbpLink;

        db.SaveChanges();
    }

    public double GetUserTotalPriceByTgId(string userTgId, Guid teamId)
    {
        var productsId = GetProductBindingsByUserTgId(userTgId, teamId)
            .Select(s => s.ProductId);

        var productPrices = productsId
            .Select(productId => GetProductByProductId(productId).Price);

        return productPrices.Sum();
    }

    public UserProductTable[] GetProductBindingsByUserTgId(string userTgId, Guid teamId)
    {
        return db.Bindings!
            .Where(userProductTable
                => userProductTable.UserTelegramId!.Equals(userTgId) && userProductTable.TeamId.Equals(teamId))
            .ToArray();
    }

    public Product GetProductByProductId(Guid id)
    {
        var product = db.Products.FirstOrDefault(productTable => productTable.TeamId.Equals(id));

        return new Product
        {
            Name = product!.Name,
            Price = product.Price,
            TotalPrice = product.TotalPrice,
            Count = product.Count
        };
    }

    public void DeleteUserProductBinding(string? userTelegramId, long teamId, Guid productId)
    {
        var binding = db.Bindings
            .FirstOrDefault(userProductTable
                => userProductTable.UserTelegramId!.Equals(userTelegramId) && userProductTable.TeamId.Equals(teamId));

        db.Bindings.Remove(binding!);
        db.SaveChanges();
    }

    public string? GetSbpLinkByUserTgId(string userTgId)
    {
        var user = db.Users.FirstOrDefault(userTable => userTable.UserTelegramId!.Equals(userTgId));

        return user?.SbpLink;
    }

    public void AddProduct(Guid id, Product productModel, Guid receiptId, string buyerTelegramId, long teamId)
    {
        var productTable = new ProductTable
        {
            Id = id,
            Name = productModel.Name,
            TotalPrice = productModel.TotalPrice,
            Price = productModel.Price,
            Count = productModel.Count,
            TeamId = teamId,
            ReceiptId = receiptId,
            BuyerTelegramId = buyerTelegramId
        };

        db.Products.Add(productTable);
        db.SaveChanges();
    }

    public void AddProducts(Guid[] ids, Product[] productModels, Guid receiptId, string buyerTelegramId, long teamId)
    {
        for (var i = 0; i < ids.Length; i++)
        {
            AddProduct(ids[i], productModels[i], receiptId, buyerTelegramId, teamId);
        }
    }

    public void AddUserProductBinding(string? userTelegramId, long teamId, Guid productId)
    {
        var binding = new UserProductTable { UserTelegramId = userTelegramId, ProductId = productId, TeamId = teamId };
        db.Bindings.Add(binding);
        db.SaveChanges();
    }
}