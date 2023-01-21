using AutoMapper;
using SqliteProvider.Models;
using SqliteProvider.Tables;

namespace SqliteProvider.Repositories.ProductRepository;

public class ProductRepository : IProductRepository
{
    private readonly IMapper mapper;
    private readonly DbContext dbContext;

    public ProductRepository(DbContext dbContext, IMapper mapper)
    {
        this.mapper = mapper;
        this.dbContext = dbContext;
    }

    public void AddProduct(Product product)
    {
        var productTable = mapper.Map<ProductTable>(product);

        dbContext.Products.Add(productTable);
        dbContext.SaveChanges();
    }

    public void AddProducts(IEnumerable<Product> productModels)
    {
        foreach (var productModel in productModels)
            AddProduct(productModel);
    }

    public IEnumerable<Product> GetProductsByTeamId(Guid teamId)
    {
        var productTables = dbContext.Products
            .Where(productTable => productTable.TeamId == teamId);

        return productTables
            .Select(productTable => mapper.Map<Product>(productTable));
    }

    public int GetAddedProductsCount(long buyerChatId, Guid teamId)
    {
        return dbContext.Products
            .Count(productTable => productTable.BuyerChatId == buyerChatId && productTable.TeamId == teamId);
    }
    
    public long GetBuyerChatId(Guid productId)
        => dbContext.Products.FirstOrDefault(productTable => productTable.Id == productId)!.BuyerChatId;

    public double GetProductTotalPriceByProductId(Guid productId)
        => dbContext.Products
            .FirstOrDefault(productTable => productTable.Id == productId)
            !.TotalPrice;
}