using Microsoft.Extensions.Configuration;
using SqliteProvider.Exceptions;
using SqliteProvider.Repositories.ProductRepository;
using SqliteProvider.Repositories.UserProductBindingRepository;
using SqliteProvider.Repositories.UserRepository;

namespace SqliteProvider.Transactions.DeleteAllTeamIdTransaction;

public class DeleteAllTeamIdTransaction : IDeleteAllTeamIdTransaction
{
    private readonly DbContext db;

    private readonly IUserRepository userRepository;
    private readonly IProductRepository productRepository;
    private readonly IUserProductBindingRepository userProductBindingRepository;

    public DeleteAllTeamIdTransaction(
        IConfiguration config,
        IUserRepository userRepository,
        IProductRepository productRepository,
        IUserProductBindingRepository userProductBindingRepository)
    {
        this.userRepository = userRepository;
        this.productRepository = productRepository;
        this.userProductBindingRepository = userProductBindingRepository;

        db = new DbContext(config.GetValue<string>("DbConnectionString"));
    }

    public void DeleteAllTeamId(Guid teamId)
    {
        using var transaction = db.Database.BeginTransaction();

        try
        {
            userRepository.DeleteAllUsersByTeamId(db, teamId);
            productRepository.DeleteAllProductsByTeamId(db, teamId);
            userProductBindingRepository.DeleteAllUserProductBindingsByTeamId(db, teamId);

            transaction.Commit();
        }
        catch (Exception)
        {
            throw new DeleteAllTeamIdException("Fail remove, db rollback");
        }
    }
}