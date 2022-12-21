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
        var userProductBindingTables = db.Bindings
            .Where(userProductTable
                => userProductTable.UserChatId.Equals(userChatId) && userProductTable.TeamId.Equals(teamId));

        return userProductBindingTables
            .Select(userProductBindingTable => mapper.Map<UserProductBinding>(userProductBindingTable));
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

    public void DeleteAllUserProductBindingsByTeamId(Guid teamId)
    {
        var bindingTables = db.Bindings
            .Where(bindingTable => bindingTable.TeamId.Equals(teamId));

        foreach (var bindingTable in bindingTables)
            db.Bindings.Remove(bindingTable);

        db.SaveChanges();
    }

    public int GetUserProductBindingCount(Guid productId)
        => db.Bindings.Count(binding => binding.ProductId.Equals(productId));
}