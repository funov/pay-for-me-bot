namespace PaymentLogic.RequisiteParser;

public interface IBankingLink
{
    static string bankingLinkRegexPattern;

    bool IsBankingLinkValid(string bankingLink);
}