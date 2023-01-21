namespace SqliteProvider.Transactions.DeleteUsersAndBindingsTransaction;

public interface IDeleteUsersAndBindingsTransaction
{
    void DeleteUsersAndBindings(long chatId, Guid teamId);
}