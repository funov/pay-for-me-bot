using System.Text.RegularExpressions;

namespace PaymentLogic.RequisiteParser.BankingLinkVerifier;

public abstract class BankingLinkVerifier
{
    protected abstract string BankingLinkRegexPattern { get; }

    public bool IsBankingLinkValid(string bankingLink)
    {
        var regex = new Regex(BankingLinkRegexPattern);
        var matches = regex.Matches(bankingLink);
        return matches.Count == 1;
    }
}