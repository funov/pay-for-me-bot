using Product = PayForMeBot.ReceiptApiClient.Models.Product;

namespace PayForMeBot.SqliteDriver;

public interface ISqliteDriver
{
    void AddUser(string userTgId, long teamId, string? spbLink = null);
    long GetTeamIdByUserTgId(string userTgId);
    void AddSbpLink(string userTgId, Guid teamId, string? sbpLink);
    void AddProduct(Guid id, Product productModel, Guid receiptId, string buyerTelegramId, long teamId);
    void AddUserProductBinding(string? userTelegramId, long teamId, Guid productId);
    void DeleteUserProductBinding(string? userTelegramId, long teamId, Guid productId);
    string? GetSbpLinkByUserTgId(string userTgId);
    double GetUserTotalPriceByTgId(string userTgId, Guid teamId);
    void AddProducts(Guid[] ids, Product[] productModels, Guid receiptId, string buyerTelegramId, long teamId);
}