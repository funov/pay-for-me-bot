namespace SqliteProvider.Repositories.UserRepository;

public interface IUserRepository
{
    void AddUser(string userTgId, long userChatId, Guid teamId);
    Guid GetTeamIdByUserChatId(long userChatId);
    bool IsUserInDb(long userChatId);

    void AddPhoneNumberAndTinkoffLink(long userChatId, Guid teamId, string? telephoneNumber,
        string? tinkoffLink = null);

    void ChangeUserStage(long userChatId, Guid teamId, string state);
    string? GetUserStage(long userChatId, Guid teamId);
    string? GetTinkoffLinkByUserChatId(long userChatId);
    bool IsUserSentRequisite(long userChatId);
    string GetUsernameByChatId(long chatId);
    string GetPhoneNumberByChatId(long chatId);
    IEnumerable<long> GetUsersChatIdInTeam(Guid teamId);
    string GetTypeRequisites(long chatId);
    bool IsAllTeamHasPhoneNumber(Guid teamId);
    void DeleteAllUsersByTeamId(Guid teamId);
}