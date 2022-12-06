using PayForMeBot.ReceiptApiClient.Models;
using PayForMeBot.SqliteDriver.Models;
using Product = PayForMeBot.ReceiptApiClient.Models.Product;

namespace PayForMeBot.SqliteDriver;

public interface ISqliteDriver
{
    void AddUser(string userTgId, Guid teamId, string? spbLink=null);
    void AddSbpLink(string userTgId, Guid teamId, string? sbpLink);
    void AddProduct(Product product, Guid receiptId, string telegramId, Guid teamId); 
    void AddProducts(Product[] products, Guid receiptId, string telegramId, Guid teamId);
    void AddUserProductBinding(string userTelegramId, Guid teamId, Guid productId);
}