namespace PaymentLogic.RequisiteParser;

public interface ITelephoneNumber
{
    string TelephoneNumberRegexPattern;

    bool IsTelephoneNumberValid(string telephoneNumber);
}