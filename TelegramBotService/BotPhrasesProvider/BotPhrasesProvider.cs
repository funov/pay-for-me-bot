using SqliteProvider.Repositories.BotPhrasesRepository;
using SqliteProvider.Types;

namespace TelegramBotService.BotPhrasesProvider;

public class BotPhrasesProvider : IBotPhrasesProvider
{
    public string? Help { get; }
    public string? WaitingOtherUsersRequisites { get; }
    public string? RequisitesSendingError { get; }
    public string? WithoutDebt { get; }
    public string? Goodbye { get; }
    public string? CreateTeamButton { get; }
    public string? JoinTeamButton { get; }
    public string? CreateOrJoinTeam { get; }
    public string? GoToSplitPurchases { get; }
    public string? TransitionToEnd { get; }
    public string? TransitionToEndYes { get; }
    public string? TransitionToEndNo { get; }
    public string? PushIfReadyToSplitPurchase { get; }
    public string? SendMeRequisites { get; }
    public string? ExampleTextProductInput { get; }
    public string? ReceiptError { get; }
    public string? ReceiptApiError { get; }
    public string? StartAddingProducts { get; }
    public string? SendMeTeamId { get; }

    public BotPhrasesProvider(IBotPhraseRepository botPhraseRepository)
    {
        Help = botPhraseRepository.GetBotPhrase(BotPhraseType.Help);
        WaitingOtherUsersRequisites = botPhraseRepository.GetBotPhrase(BotPhraseType.WaitingOtherUsersRequisites);
        RequisitesSendingError = botPhraseRepository.GetBotPhrase(BotPhraseType.RequisitesSendingError);
        WithoutDebt = botPhraseRepository.GetBotPhrase(BotPhraseType.WithoutDebt);
        Goodbye = botPhraseRepository.GetBotPhrase(BotPhraseType.Goodbye);
        CreateTeamButton = botPhraseRepository.GetBotPhrase(BotPhraseType.CreateTeamButton);
        JoinTeamButton = botPhraseRepository.GetBotPhrase(BotPhraseType.JoinTeamButton);
        CreateTeamButton = botPhraseRepository.GetBotPhrase(BotPhraseType.CreateOrJoinTeam);
        GoToSplitPurchases = botPhraseRepository.GetBotPhrase(BotPhraseType.GoToSplitPurchases);
        TransitionToEnd = botPhraseRepository.GetBotPhrase(BotPhraseType.TransitionToEnd);
        TransitionToEndYes = botPhraseRepository.GetBotPhrase(BotPhraseType.TransitionToEndYes);
        TransitionToEndNo = botPhraseRepository.GetBotPhrase(BotPhraseType.TransitionToEndNo);
        PushIfReadyToSplitPurchase = botPhraseRepository.GetBotPhrase(BotPhraseType.PushIfReadyToSplitPurchase);
        SendMeRequisites = botPhraseRepository.GetBotPhrase(BotPhraseType.SendMeRequisites);
        ExampleTextProductInput = botPhraseRepository.GetBotPhrase(BotPhraseType.ExampleTextProductInput);
        ReceiptError = botPhraseRepository.GetBotPhrase(BotPhraseType.ReceiptError);
        ReceiptApiError = botPhraseRepository.GetBotPhrase(BotPhraseType.ReceiptApiError);
        StartAddingProducts = botPhraseRepository.GetBotPhrase(BotPhraseType.StartAddingProducts);
        SendMeTeamId = botPhraseRepository.GetBotPhrase(BotPhraseType.SendMeTeamId);
        CreateOrJoinTeam = botPhraseRepository.GetBotPhrase(BotPhraseType.CreateOrJoinTeam);
    }
}