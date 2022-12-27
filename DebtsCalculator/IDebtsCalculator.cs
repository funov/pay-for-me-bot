namespace DebtsCalculator;

public interface IDebtsCalculator
{
    Dictionary<long, Dictionary<long, double>> GetUserIdToBuyerIdToDebt(Guid teamId);
}