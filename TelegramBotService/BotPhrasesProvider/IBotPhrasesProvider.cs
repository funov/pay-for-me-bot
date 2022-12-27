namespace TelegramBotService.BotPhrasesProvider;

public interface IBotPhrasesProvider
{
    string? Help { get; }
    string? WaitingOtherUsersRequisites { get; }
    string? RequisitesSendingError { get; }
    string? WithoutDebt { get; }
    string? Goodbye { get; }
    string? CreateTeamButton { get; }
    string? JoinTeamButton { get; }
    string? CreateOrJoinTeam { get; }
    string? GoToSplitPurchases { get; }
    string? TransitionToEnd { get; }
    string? TransitionToEndYes { get; }
    string? TransitionToEndNo { get; }
    string? PushIfReadyToSplitPurchase { get; }
    string? SendMeRequisites { get; }
    string? ExampleTextProductInput { get; }
    string? ReceiptError { get; }
    string? ReceiptApiError { get; }
    string? StartAddingProducts { get; }
    string? SendMeTeamId { get; }
}