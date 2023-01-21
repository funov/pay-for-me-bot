using AutoMapper;
using SqliteProvider.Models;
using SqliteProvider.Tables;

namespace SqliteProvider.Repositories.UserProductBindingRepository;

public class UserProductBindingRepository : IUserProductBindingRepository
{
    private readonly IMapper mapper;
    private readonly DbContext dbContext;

    public UserProductBindingRepository(DbContext dbContext, IMapper mapper)
    {
        this.mapper = mapper;
        this.dbContext = dbContext;
    }

    public IEnumerable<UserProductBinding> GetProductBindingsByUserChatId(long userChatId, Guid teamId)
    {
        var userProductBindingTables = dbContext.UserProductBindings
            .Where(userProductTable
                => userProductTable.UserChatId == userChatId && userProductTable.TeamId == teamId);

        return userProductBindingTables
            .Select(userProductBindingTable => mapper.Map<UserProductBinding>(userProductBindingTable));
    }

    public void DeleteUserProductBinding(long userChatId, Guid teamId, Guid productId)
    {
        var binding = dbContext.UserProductBindings
            .FirstOrDefault(userProductTable
                => userProductTable.UserChatId == userChatId
                   && userProductTable.TeamId == teamId
                   && userProductTable.ProductId == productId);

        dbContext.UserProductBindings.Remove(binding!);
        dbContext.SaveChanges();
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

        dbContext.UserProductBindings.Add(binding);
        dbContext.SaveChanges();
    }

    public int GetUserProductBindingCount(Guid productId)
        => dbContext.UserProductBindings.Count(binding => binding.ProductId == productId);
}