using System.Text.RegularExpressions;

namespace PaymentLogic.RequisiteParser.TelePhoneNumbersVerifier;

public abstract class TelePhoneNumberVerifier
{
    protected abstract string telePhoneNumberRegexPattern { get; }

    public bool IsTelePhoneNumberValid(string telePhoneNumber)
    {
        var regex = new Regex(telePhoneNumberRegexPattern);
        var matches = regex.Matches(telePhoneNumber);
        return matches.Count == 1;
    }
}