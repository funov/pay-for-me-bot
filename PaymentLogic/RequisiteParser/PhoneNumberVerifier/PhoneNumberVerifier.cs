using System.Text.RegularExpressions;

namespace PaymentLogic.RequisiteParser.PhoneNumberVerifier;

public abstract class PhoneNumberVerifier
{
    protected abstract string PhoneNumberRegexPattern { get; }

    public bool IsPhoneNumberValid(string phoneNumber)
    {
        var regex = new Regex(PhoneNumberRegexPattern);
        var matches = regex.Matches(phoneNumber);
        return matches.Count == 1;
    }
}