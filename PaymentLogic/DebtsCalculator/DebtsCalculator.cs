using SqliteProvider.Repositories.ProductRepository;
using SqliteProvider.Repositories.UserProductBindingRepository;
using SqliteProvider.Repositories.UserRepository;

namespace PaymentLogic;

public class DebtsCalculator : IDebtsCalculator
{
    private readonly IUserRepository userRepository;
    private readonly IUserProductBindingRepository userProductBindingRepository;
    private readonly IProductRepository productRepository;

    public DebtsCalculator(
        IUserRepository userRepository,
        IUserProductBindingRepository userProductBindingRepository,
        IProductRepository productRepository)
    {
        this.userRepository = userRepository;
        this.userProductBindingRepository = userProductBindingRepository;
        this.productRepository = productRepository;
    }

    public Dictionary<long, Dictionary<long, double>> GetUserIdToBuyerIdToDebt(Guid teamId)
    {
        var userIdToBuyerIdToDebt = new Dictionary<long, Dictionary<long, double>>();
        var teamUserChatIds = userRepository.GetUserChatIdsByTeamId(teamId);

        foreach (var teamUserChatId in teamUserChatIds)
        {
            var productIds = userProductBindingRepository
                .GetProductBindingsByUserChatId(teamUserChatId, teamId)
                .Select(userProductTable => userProductTable.ProductId);

            userIdToBuyerIdToDebt[teamUserChatId] = new Dictionary<long, double>();

            foreach (var productId in productIds)
            {
                var buyerChatId = productRepository.GetBuyerChatId(productId);
                var productPrice = productRepository.GetProductTotalPriceByProductId(productId);

                var amount = productPrice / userProductBindingRepository.GetUserProductBindingCount(productId);

                if (buyerChatId == teamUserChatId)
                    continue;

                if (!userIdToBuyerIdToDebt[teamUserChatId].ContainsKey(buyerChatId))
                    userIdToBuyerIdToDebt[teamUserChatId][buyerChatId] = amount;
                else
                    userIdToBuyerIdToDebt[teamUserChatId][buyerChatId] += amount;
            }
        }

        return userIdToBuyerIdToDebt;
    }
}