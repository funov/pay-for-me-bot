using SqliteProvider.Exceptions;

namespace SqliteProvider.Transactions.DeleteUsersAndBindingsTransaction;

public class DeleteUsersAndBindingsTransaction : IDeleteUsersAndBindingsTransaction
{
    private readonly DbContext dbContext;

    public DeleteUsersAndBindingsTransaction(DbContext dbContext) => this.dbContext = dbContext;

    public void DeleteUsersAndBindings(long chatId, Guid teamId)
    {
        using var transaction = dbContext.Database.BeginTransaction();

        try
        {
            DeleteUser(chatId, teamId);
            DeleteBindings(chatId, teamId);

            transaction.Commit();
        }
        catch (Exception)
        {
            throw new DeleteTransactionException("Delete fail, db rollback");
        }
    }

    private void DeleteUser(long chatId, Guid teamId)
    {
        var userTable = dbContext.Users
            .FirstOrDefault(userTable => userTable.UserChatId == chatId && userTable.TeamId == teamId);

        dbContext.Users.Remove(userTable!);
        dbContext.SaveChanges();
    }

    private void DeleteBindings(long chatId, Guid teamId)
    {
        var bindingTables = dbContext.UserProductBindings
            .Where(userTable => userTable.UserChatId == chatId && userTable.TeamId == teamId);

        foreach (var bindingTable in bindingTables)
            dbContext.UserProductBindings.Remove(bindingTable);

        dbContext.SaveChanges();
    }
}