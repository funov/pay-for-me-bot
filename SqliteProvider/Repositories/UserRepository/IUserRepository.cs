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
    
    // Guid GetTeamIdByUserChatId(long userChatId);
    // string? GetUserStage(long userChatId, Guid teamId);
    // string? GetTinkoffLinkByUserChatId(long userChatId);
    // string GetUsernameByChatId(long chatId);
    // string GetPhoneNumberByChatId(long chatId);

    void AddPhoneNumberAndTinkoffLink(long userChatId, Guid teamId, string? telephoneNumber,
        string? tinkoffLink = null);
    
    string GetRequisitesType(long chatId);
}