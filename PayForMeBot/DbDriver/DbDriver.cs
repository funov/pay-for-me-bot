using Microsoft.Extensions.Configuration;
using PayForMeBot.DbDriver.Models;
using Product = PayForMeBot.ReceiptApiClient.Models.Product;

namespace PayForMeBot.DbDriver;

public class DbDriver : IDbDriver
{
    private readonly DbContext db;
    private static HashSet<string> states = new() { "start", "middle", "end" };

    public DbDriver(IConfiguration config) => db = new DbContext(config.GetValue<string>("DbConnectionString"));

    public void AddUser(string userTgId, long userChatId, Guid teamId)
    {
        var user = new UserTable
        {
            Username = userTgId,
            TeamId = teamId,
            UserChatId = userChatId,
            Stage = "start"
        };

        db.Users.Add(user);
        db.SaveChanges();
    }

    public Guid GetTeamIdByUserChatId(long userChatId)
        => db.Users
            .FirstOrDefault(userTable => userTable.UserChatId.Equals(userChatId))
            !.TeamId;

    public bool IsUserInDb(long userChatId) =>
        db.Users.FirstOrDefault(userTable => userTable.UserChatId.Equals(userChatId)) != null;

    public void AddPhoneNumberAndTinkoffLink(long userChatId, Guid teamId, string? telephoneNumber,
        string? tinkoffLink = null)
    {
        var userTable = db.Users.FirstOrDefault(userTable
            => userTable.UserChatId.Equals(userChatId) && userTable.TeamId.Equals(teamId));

        if (userTable == null)
            throw new InvalidOperationException($"User {userChatId} not exist");

        userTable.TinkoffLink = tinkoffLink;
        userTable.TelephoneNumber = telephoneNumber;

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

    public IEnumerable<UserProductTable> GetProductBindingsByUserChatId(long userChatId, Guid teamId)
        => db.Bindings
            .Where(userProductTable
                => userProductTable.UserChatId.Equals(userChatId) && userProductTable.TeamId.Equals(teamId));

    // public Product GetProductByProductId(Guid id)
    // {
    //     var product = db.Products.FirstOrDefault(productTable => productTable.TeamId.Equals(id));
    //
    //     return new Product
    //     {
    //         Name = product!.Name,
    //         Price = product.Price,
    //         TotalPrice = product.TotalPrice,
    //         Count = product.Count
    //     };
    // }

    public IEnumerable<ProductTable> GetProductsByTeamId(Guid teamId)
        => db.Products
            .Where(productTable => productTable.TeamId == teamId);

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

    public string? GetTinkoffLinkByUserChatId(long userChatId)
        => db.Users.FirstOrDefault(userTable => userTable.UserChatId.Equals(userChatId))?.TinkoffLink;

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

    public bool IsUserSentRequisite(long userChatId)
        => db.Users
            .Where(userTable => userTable.UserChatId.Equals(userChatId)
                                && userTable.TinkoffLink != null
                                && userTable.TelephoneNumber != null)
            .ToList()
            .Count == 1;

    public Dictionary<long, double> GetRequisitesAndDebts(long chatId, Guid teamId)
    {
        var whomOwes2AmountOwedMoney = new Dictionary<long, double>();
        var productIds = GetProductBindingsByUserChatId(chatId, teamId)
            .Select(userProductTable => userProductTable.ProductId).ToList();

        foreach (var productId in productIds)
        {
            var buyerChatId = GetBuyerChatId(productId);
            var amount = db.Products.FirstOrDefault(productTable =>
                productTable.Id.Equals(productId))!.TotalPrice / CountPeopleBuyProduct(productId);
            if (whomOwes2AmountOwedMoney.ContainsKey(buyerChatId))
                whomOwes2AmountOwedMoney[buyerChatId] += amount;
            else
            {
                whomOwes2AmountOwedMoney[buyerChatId] = amount;
            }
        }

        return whomOwes2AmountOwedMoney;
    }

    public string GetUsernameByChatId(long chatId) =>
        db.Users.FirstOrDefault(userTable => userTable.UserChatId.Equals(chatId))!.Username!;

    private int CountPeopleBuyProduct(Guid productId)
        => db.Bindings.Count(binding => binding.ProductId.Equals(productId));

    private long GetBuyerChatId(Guid productId) =>
        db.Products.FirstOrDefault(productTable => productTable.Id.Equals(productId))!.BuyerChatId;

    public string GetPhoneNumberByChatId(long chatId) =>
        db.Users.FirstOrDefault(userTable => userTable.UserChatId.Equals(chatId))!.TelephoneNumber!;

    public bool DoesAllTeamUsersHavePhoneNumber(Guid teamId) =>
        db.Users.Where(userTable => userTable.TeamId.Equals(teamId))
            .Count(userTable => userTable.TelephoneNumber != null) ==
        db.Users.Count(userTable => userTable.TeamId.Equals(teamId));

    public string GetTypeRequisites(long chatId)
    {
        return db.Users.FirstOrDefault(userTable => userTable.UserChatId.Equals(chatId))?.TinkoffLink != null
            ? "tinkoffLink"
            : "phoneNumber";
    }
}