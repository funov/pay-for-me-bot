using SqliteProvider.Models;

namespace SqliteProvider.SqliteProvider;

public interface ISqliteProvider
{
    // void AddUser(string userTgId, long userChatId, Guid teamId);
    // Guid GetTeamIdByUserChatId(long userChatId);
    // bool IsUserInDb(long userChatId);
    // void ChangeUserStage(long userChatId, Guid teamId, string state);
    // void DeleteUserProductBinding(long userChatId, Guid teamId, Guid productId);
    // string? GetTinkoffLinkByUserChatId(long userChatId);
    // void AddProduct(Product product);
    // public void AddProducts(IEnumerable<Product> productModels);
    // void AddUserProductBinding(Guid id, long userChatId, Guid teamId, Guid productId);
    // IEnumerable<UserProductBinding> GetProductBindingsByUserChatId(long userChatId, Guid teamId);
    // string? GetUserStage(long userChatId, Guid teamId);
    // IEnumerable<Product> GetProductsByTeamId(Guid teamId);
    // bool IsUserSentRequisite(long userChatId);
    // Dictionary<long, Dictionary<long, double>> GetRequisitesAndDebts(Guid teamId);
    // string GetPhoneNumberByChatId(long chatId);
    // string GetUsernameByChatId(long chatId);
    // bool IsAllTeamHasPhoneNumber(Guid teamId);
    // string GetTypeRequisites(long chatId);
    // IEnumerable<long> GetUsersChatIdInTeam(Guid teamId);
    // void DeleteTeamInDb(Guid teamId);
    // void AddPhoneNumberAndTinkoffLink(long userChatId, Guid teamId, string? telephoneNumber,
    //     string? tinkoffLink = null);
}