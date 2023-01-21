namespace SqliteProvider.Exceptions;

public class DeleteTransactionException : Exception
{
    public DeleteTransactionException(string message) : base(message)
    {
    }
}