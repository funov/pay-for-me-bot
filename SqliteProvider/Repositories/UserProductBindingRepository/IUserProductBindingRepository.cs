using SqliteProvider.Models;

namespace SqliteProvider.Repositories.UserProductBindingRepository;

public interface IUserProductBindingRepository
{
    void AddUserProductBinding(Guid id, long userChatId, Guid teamId, Guid productId);
    void DeleteUserProductBinding(long userChatId, Guid teamId, Guid productId);
    IEnumerable<UserProductBinding> GetProductBindingsByUserChatId(long userChatId, Guid teamId);
    int GetUserProductBindingCount(Guid productId);
}