using System.Text.RegularExpressions;

namespace PaymentLogic.RequisiteParser.BankingLinkVerifier;

public abstract class BankingLinkVerifier
{
    protected abstract string bankingLinkRegexPattern { get; }

    public bool IsBankingLinkValid(string bankingLink)
    {
        var regex = new Regex(bankingLinkRegexPattern);
        var matches = regex.Matches(bankingLink);
        return matches.Count == 1;
    }
}