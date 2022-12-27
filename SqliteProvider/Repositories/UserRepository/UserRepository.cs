using AutoMapper;
using Microsoft.Extensions.Configuration;
using SqliteProvider.Types;
using SqliteProvider.Models;
using SqliteProvider.Tables;

namespace SqliteProvider.Repositories.UserRepository;

public class UserRepository : IUserRepository
{
    private readonly IMapper mapper;
    private readonly DbContext db;

    public UserRepository(IConfiguration config, IMapper mapper)
    {
        this.mapper = mapper;
        db = new DbContext(config.GetValue<string>("DbConnectionString"));
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

        db.Users.Add(user);
        db.SaveChanges();
    }

    public bool IsUserInDb(long userChatId)
        => db.Users.FirstOrDefault(userTable => userTable.UserChatId == userChatId) != null;

    public void AddPhoneNumber(long userChatId, string telephoneNumber)
    {
        var userTable = GetUserTable(userChatId);
        userTable.PhoneNumber = telephoneNumber;
        db.SaveChanges();
    }

    public void AddTinkoffLink(long userChatId, string tinkoffLink)
    {
        var userTable = GetUserTable(userChatId);
        userTable.TinkoffLink = tinkoffLink;
        db.SaveChanges();
    }

    private UserTable GetUserTable(long chatId)
    {
        var userTable = db.Users
            .FirstOrDefault(userTable => userTable.UserChatId == chatId);

        return userTable ?? throw new InvalidOperationException($"User {chatId} not exist");
    }

    public void ChangeUserStage(long userChatId, Guid teamId, UserStage stage)
    {
        var userTable = db.Users
            .FirstOrDefault(userTable => userTable.UserChatId == userChatId && userTable.TeamId == teamId);

        if (userTable == null)
            throw new InvalidOperationException($"User {userChatId} not exist");

        userTable.Stage = stage;
        db.SaveChanges();
    }

    public User? GetUser(long userChatId)
    {
        var userTable = db.Users
            .FirstOrDefault(table => table.UserChatId == userChatId);

        return mapper.Map<User>(userTable);
    }

    public bool IsUserSentRequisite(long userChatId)
        => db.Users
            .FirstOrDefault(userTable => userTable.UserChatId == userChatId)
            !.PhoneNumber != null;

    public IEnumerable<long> GetUserChatIdsByTeamId(Guid teamId)
        => db.Users
            .Where(userTable => userTable.TeamId == teamId)
            .Select(userTable => userTable.UserChatId);

    public RequisiteType GetRequisiteType(long chatId)
    {
        var tinkoffLink = db.Users
            .FirstOrDefault(userTable => userTable.UserChatId == chatId)
            ?.TinkoffLink;

        return tinkoffLink != null
            ? RequisiteType.PhoneNumberAndTinkoffLink
            : RequisiteType.PhoneNumber;
    }

    public bool IsAllTeamHasPhoneNumber(Guid teamId)
    {
        var hasPhoneNumberUsersCount = db.Users
            .Where(userTable => userTable.TeamId == teamId)
            .Count(userTable => userTable.PhoneNumber != null);

        var teamUsersCount = db.Users
            .Count(userTable => userTable.TeamId == teamId);

        return hasPhoneNumberUsersCount == teamUsersCount;
    }

    public void DeleteAllUsersByTeamId(Guid teamId)
    {
        var userTables = db.Users
            .Where(userTable => userTable.TeamId == teamId);

        foreach (var userTable in userTables)
            db.Users.Remove(userTable);

        db.SaveChanges();
    }
}