using PayForMeBot.ReceiptApiClient.Models;
using PayForMeBot.SqliteDriver.Models;
using Product = PayForMeBot.ReceiptApiClient.Models.Product;

namespace PayForMeBot.SqliteDriver;

public interface ISqliteDriver
{
    void AddUser(string userTgId, Guid teamId, string? spbLink=null);
    void AddSbpLink(string userTgId, Guid teamId, string? sbpLink);
    void AddProduct(Guid id, Product productModel, Guid receiptId, string buyerTelegramId, Guid teamId);

    void AddUserProductBinding(string userTelegramId, Guid teamId, Guid productId);
    string GetSbpLinkByUserTgId(string userTgId);
    double GetUserTotalPriceByTgId(string userTgId, Guid teamId);
    void AddProducts(Guid[] ids, Product[] productModels, Guid receiptId, string buyerTelegramId, Guid teamId);
}