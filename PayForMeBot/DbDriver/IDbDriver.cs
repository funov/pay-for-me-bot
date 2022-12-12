using PayForMeBot.DbDriver.Models;
using Product = PayForMeBot.ReceiptApiClient.Models.Product;

namespace PayForMeBot.DbDriver;

public interface IDbDriver
{
    void AddUser(string userTgId, long userChatId, Guid teamId);
    Guid GetTeamIdByUserChatId(long userChatId);
    bool IsUserInDb(long userChatId);
    void AddPhoneNumberAndTinkoffLink(long userChatId, Guid teamId, string? telephoneNumber,
        string? tinkoffLink = null);
    void ChangeUserStage(long userChatId, Guid teamId, string stage);
    // Product GetProductByProductId(Guid id);
    void DeleteUserProductBinding(Guid id, long userChatId, Guid teamId, Guid productId);
    string? GetTinkoffLinkByUserChatId(long userChatId);
    void AddProduct(Guid id, Product productModel, Guid receiptId, long buyerChatId, Guid teamId);
    public void AddProducts(Guid[] ids, Product[] productModels, Guid receiptId, long buyerChatId, Guid teamId);
    void AddUserProductBinding(Guid id, long userChatId, Guid teamId, Guid productId);
    IEnumerable<UserProductTable> GetProductBindingsByUserChatId(long userChatId, Guid teamId);
    string? GetUserStage(long userChatId, Guid teamId);
    IEnumerable<ProductTable> GetProductsByTeamId(Guid teamId);
    bool IsUserSentRequisite(long userChatId);
    Dictionary<long, Dictionary<long, double>> GetRequisitesAndDebts(Guid teamId);
    string GetPhoneNumberByChatId(long chatId);
    string GetUsernameByChatId(long chatId);
    bool DoesAllTeamUsersHavePhoneNumber(Guid teamId);
    string GetTypeRequisites(long chatId);
    List<long> GetUsersChatIdInTeam(Guid teamId);
    void DeleteTeamInDb(Guid teamId);
}