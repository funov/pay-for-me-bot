using AutoMapper;
using Microsoft.Extensions.Configuration;
using SqliteProvider.Models;
using SqliteProvider.Tables;

namespace SqliteProvider.Repositories.ProductRepository;

public class ProductRepository : IProductRepository
{
    private readonly IMapper mapper;
    private readonly DbContext db;

    public ProductRepository(IConfiguration config, IMapper mapper)
    {
        this.mapper = mapper;
        db = new DbContext(config.GetValue<string>("DbConnectionString"));
    }

    public void AddProduct(Product product)
    {
        var productTable = mapper.Map<ProductTable>(product);

        db.Products.Add(productTable);
        db.SaveChanges();
    }

    public void AddProducts(IEnumerable<Product> productModels)
    {
        foreach (var productModel in productModels)
            AddProduct(productModel);
    }

    public IEnumerable<Product> GetProductsByTeamId(Guid teamId)
    {
        var productTables = db.Products
            .Where(productTable => productTable.TeamId == teamId);

        return productTables
            .Select(productTable => mapper.Map<Product>(productTable));
    }

    public void DeleteAllProductsByTeamId(DbContext transactionDbContext, Guid teamId)
    {
        var productTables = transactionDbContext.Products
            .Where(productTable => productTable.TeamId == teamId);

        foreach (var productTable in productTables)
            transactionDbContext.Products.Remove(productTable);

        transactionDbContext.SaveChanges();
    }

    public long GetBuyerChatId(Guid productId)
        => db.Products.FirstOrDefault(productTable => productTable.Id == productId)!.BuyerChatId;

    public double GetProductTotalPriceByProductId(Guid productId)
        => db.Products
            .FirstOrDefault(productTable => productTable.Id == productId)
            !.TotalPrice;
}