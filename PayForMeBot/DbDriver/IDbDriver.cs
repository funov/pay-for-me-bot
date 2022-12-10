using Product = PayForMeBot.ReceiptApiClient.Models.Product;

namespace PayForMeBot.DbDriver;

public interface IDbDriver
{
    void AddUser(string userTelegramId, Guid teamId, string? spbLink = null);
    Guid GetTeamIdByUserTelegramId(string userTelegramId);
    void AddSbpLink(string userTelegramId, Guid teamId, string? sbpLink);
    void AddProduct(Guid id, Product productModel, Guid receiptId, string buyerTelegramId, Guid teamId);
    void AddUserProductBinding(string? userTelegramId, Guid teamId, Guid productId);
    void DeleteUserProductBinding(string? userTelegramId, Guid teamId, Guid productId);
    string? GetSbpLinkByUserTelegramId(string userTelegramId);
    double GetUserTotalPriceByTelegramId(string userTelegramId, Guid teamId);
    void AddProducts(Guid[] ids, Product[] productModels, Guid receiptId, string buyerTelegramId, Guid teamId);
}