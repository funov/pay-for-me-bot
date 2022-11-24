namespace PayForMeBot.SqliteDriver;

public interface ISqliteDriver
{
    // Таблица пользователей
    // Таблица продуктов
    // Таблица сопоставления продуктов и пользователей

    void AddUser(string telegramId, Guid teamId);
    void AddSbpLink(string sbpLink);
    void AddProduct(object product, Guid teamId, Guid receiptId, string buyerTelegramId); // product = id, name, price
    void AddProducts(object[] products, Guid teamId, Guid receiptId, string buyerTelegramId);
    void AddUserProductBinding(string userTelegramId, Guid productId, Guid teamId);
}