using SqliteProvider.Models;

namespace SqliteProvider.Repositories.UserRepository;

public interface IUserRepository
{
    void AddUser(string userTgId, long userChatId, Guid teamId);
    bool IsUserInDb(long userChatId);
    void ChangeUserStage(long userChatId, Guid teamId, string state);
    bool IsUserSentRequisite(long userChatId);
    IEnumerable<long> GetUserChatIdsByTeamId(Guid teamId);
    bool IsAllTeamHasPhoneNumber(Guid teamId);
    void DeleteAllUsersByTeamId(Guid teamId);
    User GetUser(long userChatId);
    void AddPhoneNumber(long userChatId, string telephoneNumber);
    void AddTinkoffLink(long userChatId, string tinkoffLink);
    RequisiteType GetRequisiteType(long chatId);
}