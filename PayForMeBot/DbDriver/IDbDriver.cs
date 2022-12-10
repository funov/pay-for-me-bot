using Product = PayForMeBot.ReceiptApiClient.Models.Product;

namespace PayForMeBot.DbDriver;

public interface IDbDriver
{
    void AddUser(string userTgId, long userChatId, Guid teamId, string? spbLink);
    Guid GetTeamIdByUserChatId(long userChatId);
    void AddSbpLink(long userChatId, Guid teamId, string? sbpLink);
    void ChangeUserState(long userChatId, Guid teamId, string state);
    double GetUserTotalPriceByChatId(long userChatId, Guid teamId);
    Product GetProductByProductId(Guid id);
    void DeleteUserProductBinding(long userChatId, Guid teamId, Guid productId);
    string? GetSbpLinkByUserChatId(long userChatId);
    void AddProduct(Guid id, Product productModel, Guid receiptId, long buyerChatId, Guid teamId);
    public void AddProducts(Guid[] ids, Product[] productModels, Guid receiptId, long buyerChatId, Guid teamId);
    void AddUserProductBinding(long userChatId, Guid teamId, Guid productId);
}