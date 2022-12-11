using PayForMeBot.DbDriver.Models;
using Product = PayForMeBot.ReceiptApiClient.Models.Product;

namespace PayForMeBot.DbDriver;

public interface IDbDriver
{
    void AddUser(string userTgId, long userChatId, Guid teamId, string? spbLink);
    Guid GetTeamIdByUserChatId(long userChatId);
    bool IsUserInDb(long userChatId);
    void AddSbpLink(long userChatId, Guid teamId, string? sbpLink);
    void ChangeUserStage(long userChatId, Guid teamId, string stage);
    double GetUserTotalPriceByChatId(long userChatId, Guid teamId);
    Product GetProductByProductId(Guid id);
    void DeleteUserProductBinding(long userChatId, Guid teamId, Guid productId);
    string? GetSbpLinkByUserChatId(long userChatId);
    void AddProduct(Guid id, Product productModel, Guid receiptId, long buyerChatId, Guid teamId);
    public void AddProducts(Guid[] ids, Product[] productModels, Guid receiptId, long buyerChatId, Guid teamId);
    void AddUserProductBinding(long userChatId, Guid teamId, Guid productId);
    IEnumerable<UserProductTable> GetProductBindingsByUserChatId(long userChatId, Guid teamId);
    string? GetUserStage(long userChatId, Guid teamId);
    IEnumerable<ProductTable> GetProductsByTeamId(Guid teamId);
}