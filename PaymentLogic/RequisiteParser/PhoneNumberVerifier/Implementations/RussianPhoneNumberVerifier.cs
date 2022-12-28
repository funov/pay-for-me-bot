namespace PaymentLogic.RequisiteParser.PhoneNumberVerifier.Implementations;

public class RussianPhoneNumberVerifier : PhoneNumberVerifier
{
    protected override string PhoneNumberRegexPattern => @"^((7|8|\+7)[\- ]?)(\(?\d{3}\)?[\- ]?)?[\d\- ]{7,10}$";
}