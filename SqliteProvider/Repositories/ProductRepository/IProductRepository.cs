using SqliteProvider.Models;

namespace SqliteProvider.Repositories.ProductRepository;

public interface IProductRepository
{
    void AddProduct(Product product);
    void AddProducts(IEnumerable<Product> productModels);
    IEnumerable<Product> GetProductsByTeamId(Guid teamId);
    long GetBuyerChatId(Guid productId);
    double GetProductTotalPriceByProductId(Guid productId);
    int GetAddedProductsCount(long buyerChatId, Guid teamId);
}