using AutoMapper;
using Microsoft.Extensions.Configuration;
using SqliteProvider.Models;
using SqliteProvider.Tables;

namespace SqliteProvider.Repositories.UserProductBindingRepository;

public class UserProductBindingRepository : IUserProductBindingRepository
{
    private readonly IMapper mapper;
    private readonly DbContext db;

    public UserProductBindingRepository(IConfiguration config, IMapper mapper)
    {
        this.mapper = mapper;
        db = new DbContext(config.GetValue<string>("DbConnectionString"));
    }

    public IEnumerable<UserProductBinding> GetProductBindingsByUserChatId(long userChatId, Guid teamId)
    {
        var userProductBindingTables = db.UserProductBindings
            .Where(userProductTable
                => userProductTable.UserChatId == userChatId && userProductTable.TeamId == teamId);

        return userProductBindingTables
            .Select(userProductBindingTable => mapper.Map<UserProductBinding>(userProductBindingTable));
    }

    public void DeleteUserProductBinding(long userChatId, Guid teamId, Guid productId)
    {
        var binding = db.UserProductBindings
            .FirstOrDefault(userProductTable
                => userProductTable.UserChatId == userChatId
                   && userProductTable.TeamId == teamId
                   && userProductTable.ProductId == productId);

        db.UserProductBindings.Remove(binding!);
        db.SaveChanges();
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

        db.UserProductBindings.Add(binding);
        db.SaveChanges();
    }

    public void DeleteAllUserProductBindingsByTeamId(Guid teamId)
    {
        var bindingTables = db.UserProductBindings
            .Where(bindingTable => bindingTable.TeamId == teamId);

        foreach (var bindingTable in bindingTables)
            db.UserProductBindings.Remove(bindingTable);

        db.SaveChanges();
    }

    public int GetUserProductBindingCount(Guid productId)
        => db.UserProductBindings.Count(binding => binding.ProductId == productId);
}