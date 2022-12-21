using AutoMapper;
using Microsoft.Extensions.Configuration;
using SqliteProvider.Models;
using SqliteProvider.Tables;

namespace SqliteProvider.SqliteProvider;

public class SqliteProvider : ISqliteProvider
{
    private readonly IMapper mapper;
    private readonly DbContext db;

    public SqliteProvider(IConfiguration config, IMapper mapper)
    {
        this.mapper = mapper;
        db = new DbContext(config.GetValue<string>("DbConnectionString"));
    }

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

    public void DeleteTeamInDb(Guid teamId)
    {
        // void DeleteAllUsersByTeamId(Guid teamId)

        // void DeleteAllProductsByTeamId(Guid teamId)

        // void DeleteAllUserProductBindingsByTeamId(Guid teamId)
    }
}