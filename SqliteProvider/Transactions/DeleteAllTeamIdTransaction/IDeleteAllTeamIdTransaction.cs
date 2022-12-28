namespace SqliteProvider.Transactions.DeleteAllTeamIdTransaction;

public interface IDeleteAllTeamIdTransaction
{
    void DeleteAllTeamId(Guid teamId);
}