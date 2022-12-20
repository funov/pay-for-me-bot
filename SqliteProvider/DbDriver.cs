using Microsoft.Extensions.Configuration;
using SqliteProvider.Models;
using SqliteProvider.Tables;

namespace SqliteProvider;

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
            Stage = "start",
        };

        db.Users.Add(user);
        db.SaveChanges();
    }

    public Guid GetTeamIdByUserChatId(long userChatId)
        => db.Users
            .FirstOrDefault(userTable => userTable.UserChatId.Equals(userChatId))
            !.TeamId;

    public bool IsUserInDb(long userChatId)
        => db.Users.FirstOrDefault(userTable => userTable.UserChatId.Equals(userChatId)) != null;

    public void AddPhoneNumberAndTinkoffLink(long userChatId, Guid teamId, string? telephoneNumber,
        string? tinkoffLink = null)
    {
        var userTable = db.Users.FirstOrDefault(userTable
            => userTable.UserChatId.Equals(userChatId) && userTable.TeamId.Equals(teamId));

        if (userTable == null)
            throw new InvalidOperationException($"User {userChatId} not exist");

        userTable.TinkoffLink = tinkoffLink;
        userTable.PhoneNumber = telephoneNumber;

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
                => userProductTable.UserChatId.Equals(userChatId) && userProductTable.TeamId.Equals(teamId))
            .ToList();

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

    public void AddUserProductBinding(Guid id, long userChatId, Guid teamId, Guid productId)
    {
        var binding = new UserProductTable
        {
            Id = id,
            UserChatId = userChatId,
            ProductId = productId,
            TeamId = teamId
        };
        db.Bindings.Add(binding);
        db.SaveChanges();
    }

    public bool IsUserSentRequisite(long userChatId)
        => db.Users
            .FirstOrDefault(userTable => userTable.UserChatId.Equals(userChatId))
            !.PhoneNumber != null;

    public Dictionary<long, Dictionary<long, double>> GetRequisitesAndDebts(Guid teamId)
    {
        var whomOwes2AmountOwedMoney = new Dictionary<long, Dictionary<long, double>>();
        var teamUserChatIds = GetUsersChatIdInTeam(teamId);

        foreach (var teamUserChatId in teamUserChatIds)
        {
            var productIds = GetProductBindingsByUserChatId(teamUserChatId, teamId)
                .Select(userProductTable => userProductTable.ProductId).ToList();
            whomOwes2AmountOwedMoney[teamUserChatId] = new Dictionary<long, double>();
            foreach (var productId in productIds)
            {
                var buyerChatId = GetBuyerChatId(productId);
                var amount = db.Products.FirstOrDefault(productTable
                    => productTable.Id.Equals(productId))!.TotalPrice / CountPeopleBuyProduct(productId);
                if (buyerChatId == teamUserChatId)
                {
                    continue;
                }

                if (!whomOwes2AmountOwedMoney[teamUserChatId].ContainsKey(buyerChatId))
                {
                    whomOwes2AmountOwedMoney[teamUserChatId][buyerChatId] = amount;
                }
                else
                {
                    whomOwes2AmountOwedMoney[teamUserChatId][buyerChatId] += amount;
                }
            }
        }

        return whomOwes2AmountOwedMoney;
    }

    public string GetUsernameByChatId(long chatId)
        => db.Users.FirstOrDefault(userTable => userTable.UserChatId.Equals(chatId))!.Username!;

    private int CountPeopleBuyProduct(Guid productId)
        => db.Bindings.Count(binding => binding.ProductId.Equals(productId));

    private long GetBuyerChatId(Guid productId)
        => db.Products.FirstOrDefault(productTable => productTable.Id.Equals(productId))!.BuyerChatId;

    public string GetPhoneNumberByChatId(long chatId)
        => db.Users.FirstOrDefault(userTable => userTable.UserChatId.Equals(chatId))!.PhoneNumber!;

    public bool IsAllTeamHasPhoneNumber(Guid teamId)
    {
        var hasPhoneNumberUsersCount = db.Users
            .Where(userTable => userTable.TeamId.Equals(teamId))
            .Count(userTable => userTable.PhoneNumber != null);

        var teamUsersCount = db.Users
            .Count(userTable => userTable.TeamId.Equals(teamId));

        return hasPhoneNumberUsersCount == teamUsersCount;
    }

    public string GetTypeRequisites(long chatId)
    {
        return db.Users.FirstOrDefault(userTable => userTable.UserChatId.Equals(chatId))?.TinkoffLink != null
            ? "tinkoffLink"
            : "phoneNumber";
    }

    public List<long> GetUsersChatIdInTeam(Guid teamId)
        => db.Users
            .Where(userTable => userTable.TeamId.Equals(teamId))
            .Select(userTable => userTable.UserChatId).ToList();

    public void DeleteTeamInDb(Guid teamId)
    {
        var userTables = db.Users.Where(userTable => userTable.TeamId.Equals(teamId)).ToList();
        foreach (var userTable in userTables)
        {
            db.Users.Remove(userTable);
        }

        var productTables = db.Products.Where(productTable => productTable.TeamId.Equals(teamId)).ToList();
        foreach (var productTable in productTables)
        {
            db.Products.Remove(productTable);
        }

        var bindingTables = db.Bindings.Where(bindingTable => bindingTable.TeamId.Equals(teamId)).ToList();
        foreach (var bindingTable in bindingTables)
        {
            db.Bindings.Remove(bindingTable);
        }

        db.SaveChanges();
    }
}