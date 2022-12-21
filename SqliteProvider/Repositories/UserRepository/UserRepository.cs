using AutoMapper;
using Microsoft.Extensions.Configuration;
using SqliteProvider.Models;
using SqliteProvider.Tables;

namespace SqliteProvider.Repositories.UserRepository;

public class UserRepository : IUserRepository
{
    private readonly IMapper mapper;
    private readonly DbContext db;

    private static HashSet<string> states = new() { "start", "middle", "end" };

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
            Stage = "start",
        };

        db.Users.Add(user);
        db.SaveChanges();
    }

    public bool IsUserInDb(long userChatId)
        => db.Users.FirstOrDefault(userTable => userTable.UserChatId.Equals(userChatId)) != null;

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

    public void ChangeUserStage(long userChatId, Guid teamId, string state)
    {
        if (!states.Contains(state))
            throw new InvalidOperationException($"Incorrect state {state}");

        var userTable = db.Users
            .FirstOrDefault(userTable => userTable.UserChatId.Equals(userChatId) && userTable.TeamId.Equals(teamId));

        if (userTable == null)
            throw new InvalidOperationException($"User {userChatId} not exist");

        userTable.Stage = state;
        db.SaveChanges();
    }

    public User GetUser(long userChatId)
    {
        var userTable = db.Users
            .FirstOrDefault(table => table.UserChatId == userChatId);

        return mapper.Map<User>(userTable);
    }

    public bool IsUserSentRequisite(long userChatId)
        => db.Users
            .FirstOrDefault(userTable => userTable.UserChatId.Equals(userChatId))
            !.PhoneNumber != null;

    public IEnumerable<long> GetUserChatIdsByTeamId(Guid teamId)
        => db.Users
            .Where(userTable => userTable.TeamId.Equals(teamId))
            .Select(userTable => userTable.UserChatId);

    public string GetRequisitesType(long chatId)
    {
        var tinkoffLink = db.Users
            .FirstOrDefault(userTable => userTable.UserChatId.Equals(chatId))
            ?.TinkoffLink;

        return tinkoffLink != null
            ? "tinkoffLink"
            : "phoneNumber";
    }

    public bool IsAllTeamHasPhoneNumber(Guid teamId)
    {
        var hasPhoneNumberUsersCount = db.Users
            .Where(userTable => userTable.TeamId.Equals(teamId))
            .Count(userTable => userTable.PhoneNumber != null);

        var teamUsersCount = db.Users
            .Count(userTable => userTable.TeamId.Equals(teamId));

        return hasPhoneNumberUsersCount == teamUsersCount;
    }

    public void DeleteAllUsersByTeamId(Guid teamId)
    {
        var userTables = db.Users.Where(userTable => userTable.TeamId.Equals(teamId)).ToList();

        foreach (var userTable in userTables)
        {
            db.Users.Remove(userTable);
        }

        db.SaveChanges();
    }
}