using AutoMapper;
using Microsoft.Extensions.Configuration;
using SqliteProvider.Models;
using SqliteProvider.Tables;

namespace SqliteProvider.SqliteProvider;

public class SqliteProvider : ISqliteProvider
{
    private readonly IMapper mapper;
    private readonly DbContext db;
    private static HashSet<string> states = new() { "start", "middle", "end" };

    public SqliteProvider(IConfiguration config, IMapper mapper)
    {
        this.mapper = mapper;
        db = new DbContext(config.GetValue<string>("DbConnectionString"));
    }

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

    public void ChangeUserStage(long userChatId, Guid teamId, string state)
    {
        if (!states.Contains(state))
            throw new InvalidOperationException($"Incorrect state {state}");

        var userTable = db.Users
            .FirstOrDefault(userTable => userTable.UserChatId.Equals(userChatId) && userTable.TeamId.Equals(teamId));

        if (userTable == null)
            throw new InvalidOperationException($"User {userChatId} not exist");

        userTable.Stage = state;
        db.SaveChanges();
    }

    public string? GetUserStage(long userChatId, Guid teamId)
        => db.Users
            .FirstOrDefault(x => x.UserChatId == userChatId && x.TeamId == teamId)?
            .Stage;


    public IEnumerable<UserProductBinding> GetProductBindingsByUserChatId(long userChatId, Guid teamId)
    {
        var userProductBindingTables = db.Bindings
            .Where(userProductTable
                => userProductTable.UserChatId.Equals(userChatId) && userProductTable.TeamId.Equals(teamId));

        return userProductBindingTables
            .Select(userProductBindingTable => mapper.Map<UserProductBinding>(userProductBindingTable));
    }

    public IEnumerable<Product> GetProductsByTeamId(Guid teamId)
    {
        var productTables = db.Products
            .Where(productTable => productTable.TeamId == teamId);

        return productTables
            .Select(productTable => mapper.Map<Product>(productTable));
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

    public string? GetTinkoffLinkByUserChatId(long userChatId)
        => db.Users.FirstOrDefault(userTable => userTable.UserChatId.Equals(userChatId))?.TinkoffLink;

    public void AddProduct(Guid id, Product product, Guid receiptId, long buyerChatId, Guid teamId)
    {
        var productTable = new ProductTable
        {
            Id = id,
            Name = product.Name,
            TotalPrice = product.TotalPrice,
            Price = product.Price,
            Count = product.Count,
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
        var binding = new UserProductBindingTable
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
        var whomOwesToAmountOwedMoney = new Dictionary<long, Dictionary<long, double>>();
        var teamUserChatIds = GetUsersChatIdInTeam(teamId);

        foreach (var teamUserChatId in teamUserChatIds)
        {
            var productIds = GetProductBindingsByUserChatId(teamUserChatId, teamId)
                .Select(userProductTable => userProductTable.ProductId).ToList();

            whomOwesToAmountOwedMoney[teamUserChatId] = new Dictionary<long, double>();

            foreach (var productId in productIds)
            {
                var buyerChatId = GetBuyerChatId(productId);
                var productPrice = db.Products
                    .FirstOrDefault(productTable => productTable.Id.Equals(productId))
                    !.TotalPrice;

                var amount = productPrice / GetUserProductBindingCount(productId);

                if (buyerChatId == teamUserChatId)
                    continue;

                if (!whomOwesToAmountOwedMoney[teamUserChatId].ContainsKey(buyerChatId))
                    whomOwesToAmountOwedMoney[teamUserChatId][buyerChatId] = amount;
                else
                    whomOwesToAmountOwedMoney[teamUserChatId][buyerChatId] += amount;
            }
        }

        return whomOwesToAmountOwedMoney;
    }

    public string GetUsernameByChatId(long chatId)
        => db.Users.FirstOrDefault(userTable => userTable.UserChatId.Equals(chatId))!.Username!;

    private int GetUserProductBindingCount(Guid productId)
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
        var tinkoffLink = db.Users
            .FirstOrDefault(userTable => userTable.UserChatId.Equals(chatId))
            ?.TinkoffLink;

        return tinkoffLink != null
            ? "tinkoffLink"
            : "phoneNumber";
    }

    public IEnumerable<long> GetUsersChatIdInTeam(Guid teamId)
        => db.Users
            .Where(userTable => userTable.TeamId.Equals(teamId))
            .Select(userTable => userTable.UserChatId);

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