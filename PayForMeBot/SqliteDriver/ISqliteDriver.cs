using PayForMeBot.ReceiptApiClient.Models;
using PayForMeBot.SqliteDriver.Models;
using Product = PayForMeBot.ReceiptApiClient.Models.Product;

namespace PayForMeBot.SqliteDriver;

public interface ISqliteDriver
{
    void AddUser(string telegramId, Guid teamId, string spbLink="");
    void AddSbpLink(string telegramId, Guid teamId, string sbpLink);
    void AddProduct(Product product, Guid receiptId, string telegramId, Guid teamId); 
    void AddProducts(Product[] products, Guid receiptId, string telegramId, Guid teamId);
    void AddUserProductBinding(string telegramId, Guid teamId, Guid productId);
}