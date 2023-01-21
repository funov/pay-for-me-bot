using SqliteProvider.Exceptions;

namespace SqliteProvider.Transactions.DeleteAllTeamIdTransaction;

public class DeleteAllTeamIdTransaction : IDeleteAllTeamIdTransaction
{
    private readonly DbContext dbContext;

    public DeleteAllTeamIdTransaction(DbContext dbContext) => this.dbContext = dbContext;

    public void DeleteAllTeamId(Guid teamId)
    {
        using var transaction = dbContext.Database.BeginTransaction();

        try
        {
            DeleteAllUsersByTeamId(teamId);
            DeleteAllProductsByTeamId(teamId);
            DeleteAllUserProductBindingsByTeamId(teamId);

            transaction.Commit();
        }
        catch (Exception)
        {
            throw new DeleteTransactionException("Delete fail, db rollback");
        }
    }

    private void DeleteAllUsersByTeamId(Guid teamId)
    {
        var userTables = dbContext.Users
            .Where(userTable => userTable.TeamId == teamId);

        foreach (var userTable in userTables)
            dbContext.Users.Remove(userTable);

        dbContext.SaveChanges();
    }

    private void DeleteAllProductsByTeamId(Guid teamId)
    {
        var productTables = dbContext.Products
            .Where(productTable => productTable.TeamId == teamId);

        foreach (var productTable in productTables)
            dbContext.Products.Remove(productTable);

        dbContext.SaveChanges();
    }

    private void DeleteAllUserProductBindingsByTeamId(Guid teamId)
    {
        var bindingTables = dbContext.UserProductBindings
            .Where(bindingTable => bindingTable.TeamId == teamId);

        foreach (var bindingTable in bindingTables)
            dbContext.UserProductBindings.Remove(bindingTable);

        dbContext.SaveChanges();
    }
}