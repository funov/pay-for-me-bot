using AutoMapper;
using SqliteProvider.Types;
using SqliteProvider.Models;
using SqliteProvider.Tables;

namespace SqliteProvider.Repositories.UserRepository;

public class UserRepository : IUserRepository
{
    private readonly IMapper mapper;
    private readonly DbContext dbContext;

    public UserRepository(DbContext dbContext, IMapper mapper)
    {
        this.mapper = mapper;
        this.dbContext = dbContext;
    }

    public void AddUser(string userTgId, long userChatId, Guid teamId)
    {
        var user = new UserTable
        {
            Username = userTgId,
            TeamId = teamId,
            UserChatId = userChatId,
            Stage = UserStage.TeamAddition,
        };

        dbContext.Users.Add(user);
        dbContext.SaveChanges();
    }

    public void AddPhoneNumber(long userChatId, string telephoneNumber)
    {
        var userTable = GetUserTable(userChatId);
        userTable.PhoneNumber = telephoneNumber;
        dbContext.SaveChanges();
    }

    public void AddTinkoffLink(long userChatId, string tinkoffLink)
    {
        var userTable = GetUserTable(userChatId);
        userTable.TinkoffLink = tinkoffLink;
        dbContext.SaveChanges();
    }

    private UserTable GetUserTable(long chatId)
    {
        var userTable = dbContext.Users
            .FirstOrDefault(userTable => userTable.UserChatId == chatId);

        return userTable ?? throw new InvalidOperationException($"User {chatId} not exist");
    }

    public void ChangeUserStage(long userChatId, Guid teamId, UserStage stage)
    {
        var userTable = dbContext.Users
            .FirstOrDefault(userTable => userTable.UserChatId == userChatId && userTable.TeamId == teamId);

        if (userTable == null)
            throw new InvalidOperationException($"User {userChatId} not exist");

        userTable.Stage = stage;
        dbContext.SaveChanges();
    }

    public User? GetUser(long userChatId)
    {
        var userTable = dbContext.Users
            .FirstOrDefault(table => table.UserChatId == userChatId);

        return mapper.Map<User>(userTable);
    }

    public bool IsUserSentRequisite(long userChatId)
        => dbContext.Users
            .FirstOrDefault(userTable => userTable.UserChatId == userChatId)
            !.PhoneNumber != null;

    public IEnumerable<long> GetUserChatIdsByTeamId(Guid teamId)
        => dbContext.Users
            .Where(userTable => userTable.TeamId == teamId)
            .Select(userTable => userTable.UserChatId);

    public RequisiteType GetRequisiteType(long chatId)
    {
        var tinkoffLink = dbContext.Users
            .FirstOrDefault(userTable => userTable.UserChatId == chatId)
            ?.TinkoffLink;

        return tinkoffLink != null
            ? RequisiteType.PhoneNumberAndTinkoffLink
            : RequisiteType.PhoneNumber;
    }

    public bool IsAllTeamHasPhoneNumber(Guid teamId)
    {
        var hasPhoneNumberUsersCount = dbContext.Users
            .Where(userTable => userTable.TeamId == teamId)
            .Count(userTable => userTable.PhoneNumber != null);

        var teamUsersCount = dbContext.Users
            .Count(userTable => userTable.TeamId == teamId);

        return hasPhoneNumberUsersCount == teamUsersCount;
    }
}