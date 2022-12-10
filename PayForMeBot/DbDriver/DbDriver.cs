using Microsoft.Extensions.Configuration;
using PayForMeBot.DbDriver.Models;
using Product = PayForMeBot.ReceiptApiClient.Models.Product;

namespace PayForMeBot.DbDriver;

public class DbDriver : IDbDriver
{
    private readonly DbContext db;
    private static HashSet<string> states = new() { "start", "middle", "end" };

    public DbDriver(IConfiguration config) => db = new DbContext(config.GetValue<string>("DbConnectionString"));

    public void AddUser(string userTgId, Guid teamId, string? spbLink)
    {
        var user = new UserTable { UserTelegramId = userTgId, TeamId = teamId, SbpLink = spbLink, Stage = "start" };

        db.Users.Add(user);
        db.SaveChanges();
    }

    public Guid GetTeamIdByUserTelegramId(string userTelegramId)
        => db.Users
            .FirstOrDefault(userTable => userTable.UserTelegramId!.Equals(userTelegramId))
            !.TeamId;

    public void AddSbpLink(string userTelegramId, Guid teamId, string? sbpLink)
    {
        var userTable = db.Users.FirstOrDefault(userTable
            => userTable.UserTelegramId!.Equals(userTelegramId) && userTable.TeamId.Equals(teamId));

        if (userTable == null)
            throw new InvalidOperationException($"User {userTelegramId} not exist");

        userTable.SbpLink = sbpLink;
        db.SaveChanges();
    }

    public void ChangeUserState(string userTelegramId, Guid teamId, string state)
    {
        if (!states.Contains(state))
            throw new InvalidOperationException($"Incorrect state {state}");

        var userTable = db.Users.FirstOrDefault(userTable
            => userTable.UserTelegramId!.Equals(userTelegramId) && userTable.TeamId.Equals(teamId));

        if (userTable == null)
            throw new InvalidOperationException($"User {userTelegramId} not exist");

        userTable.Stage = state;
        db.SaveChanges();
    }

    public double GetUserTotalPriceByTelegramId(string userTelegramId, Guid teamId)
        => GetProductBindingsByUserTgId(userTelegramId, teamId)
            .Select(s => s.ProductId)
            .Select(productId => GetProductByProductId(productId).Price)
            .Sum();

    private IEnumerable<UserProductTable> GetProductBindingsByUserTgId(string userTgId, Guid teamId)
        => db.Bindings
            .Where(userProductTable
                => userProductTable.UserTelegramId!.Equals(userTgId) && userProductTable.TeamId.Equals(teamId));

    private Product GetProductByProductId(Guid id)
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

    public void DeleteUserProductBinding(string? userTelegramId, Guid teamId, Guid productId)
    {
        var binding = db.Bindings
            .FirstOrDefault(userProductTable
                => userProductTable.UserTelegramId!.Equals(userTelegramId) && userProductTable.TeamId.Equals(teamId));

        db.Bindings.Remove(binding!);
        db.SaveChanges();
    }

    public string? GetSbpLinkByUserTelegramId(string userTelegramId)
        => db.Users.FirstOrDefault(userTable => userTable.UserTelegramId!.Equals(userTelegramId))?.SbpLink;

    public void AddProduct(Guid id, Product productModel, Guid receiptId, string buyerTelegramId, Guid teamId)
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

    public void AddProducts(Guid[] ids, Product[] productModels, Guid receiptId, string buyerTelegramId, Guid teamId)
    {
        for (var i = 0; i < ids.Length; i++)
        {
            AddProduct(ids[i], productModels[i], receiptId, buyerTelegramId, teamId);
        }
    }

    public void AddUserProductBinding(string? userTelegramId, Guid teamId, Guid productId)
    {
        var binding = new UserProductTable { UserTelegramId = userTelegramId, ProductId = productId, TeamId = teamId };
        db.Bindings.Add(binding);
        db.SaveChanges();
    }
}