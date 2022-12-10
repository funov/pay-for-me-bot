using Microsoft.Extensions.Configuration;
using PayForMeBot.DbDriver.Models;
using Product = PayForMeBot.ReceiptApiClient.Models.Product;

namespace PayForMeBot.DbDriver;

public class DbDriver : IDbDriver
{
    private readonly DbContext db;
    private static HashSet<string> states = new() { "start", "middle", "end" };

    public DbDriver(IConfiguration config) => db = new DbContext(config.GetValue<string>("DbConnectionString"));

    public void AddUser(string userTgId, long userChatId, Guid teamId, string? spbLink)
    {
        var user = new UserTable
        {
            UserTelegramId = userTgId,
            TeamId = teamId,
            UserChatId = userChatId,
            SbpLink = spbLink,
            Stage = "start"
        };

        db.Users.Add(user);
        db.SaveChanges();
    }

    public Guid GetTeamIdByUserChatId(long userChatId)
        => db.Users
            .FirstOrDefault(userTable => userTable.UserChatId.Equals(userChatId))
            !.TeamId;

    public void AddSbpLink(long userChatId, Guid teamId, string? sbpLink)
    {
        var userTable = db.Users.FirstOrDefault(userTable
            => userTable.UserChatId.Equals(userChatId) && userTable.TeamId.Equals(teamId));

        if (userTable == null)
            throw new InvalidOperationException($"User {userChatId} not exist");

        userTable.SbpLink = sbpLink;
        db.SaveChanges();
    }

    public void ChangeUserStage(long userChatId, Guid teamId, string stage)
    {
        if (!states.Contains(stage))
            throw new InvalidOperationException($"Incorrect state {stage}");

        var userTable = db.Users.FirstOrDefault(userTable
            => userTable.UserChatId.Equals(userChatId) && userTable.TeamId.Equals(teamId));

        if (userTable == null)
            throw new InvalidOperationException($"User {userChatId} not exist");

        userTable.Stage = stage;
        db.SaveChanges();
    }

    public string? GetUserStage(long userChatId, Guid teamId)
        => db.Users
            .FirstOrDefault(x => x.UserChatId == userChatId && x.TeamId == teamId)?
            .Stage;

    public double GetUserTotalPriceByChatId(long userChatId, Guid teamId)
        => GetProductBindingsByUserChatId(userChatId, teamId)
            .Select(userProductTable => userProductTable.ProductId)
            .Select(productId => GetProductByProductId(productId).Price)
            .Sum();

    public IEnumerable<UserProductTable> GetProductBindingsByUserChatId(long userChatId, Guid teamId)
        => db.Bindings
            .Where(userProductTable
                => userProductTable.UserChatId.Equals(userChatId) && userProductTable.TeamId.Equals(teamId));

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

    public void DeleteUserProductBinding(long userChatId, Guid teamId, Guid productId)
    {
        var binding = db.Bindings
            .FirstOrDefault(userProductTable
                => userProductTable.UserChatId.Equals(userChatId)
                   && userProductTable.TeamId.Equals(teamId)
                   && userProductTable.ProductId.Equals(productId));

        db.Bindings.Remove(binding!);
        db.SaveChanges();
    }

    public string? GetSbpLinkByUserChatId(long userChatId)
        => db.Users.FirstOrDefault(userTable => userTable.UserChatId.Equals(userChatId))?.SbpLink;

    public void AddProduct(Guid id, Product productModel, Guid receiptId, long buyerChatId, Guid teamId)
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
            BuyerChatId = buyerChatId
        };

        db.Products.Add(productTable);
        db.SaveChanges();
    }

    public void AddProducts(Guid[] ids, Product[] productModels, Guid receiptId, long buyerChatId, Guid teamId)
    {
        for (var i = 0; i < ids.Length; i++)
        {
            AddProduct(ids[i], productModels[i], receiptId, buyerChatId, teamId);
        }
    }

    public void AddUserProductBinding(long userChatId, Guid teamId, Guid productId)
    {
        var binding = new UserProductTable { UserChatId = userChatId, ProductId = productId, TeamId = teamId };
        db.Bindings.Add(binding);
        db.SaveChanges();
    }
}