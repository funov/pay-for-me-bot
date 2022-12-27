namespace SqliteProvider.Types;

public enum BotPhraseType
{
    Help,
    WaitingOtherUsersRequisites,
    RequisitesSendingError,
    WithoutDebt,
    Goodbye,
    CreateTeamButton,
    JoinTeamButton,
    CreateOrJoinTeam,
    GoToSplitPurchases,
    TransitionToEnd,
    TransitionToEndYes,
    TransitionToEndNo,
    PushIfReadyToSplitPurchase,
    SendMeRequisites,
    ExampleTextProductInput,
    ReceiptError,
    ReceiptApiError,
    StartAddingProducts,
    SendMeTeamId
}