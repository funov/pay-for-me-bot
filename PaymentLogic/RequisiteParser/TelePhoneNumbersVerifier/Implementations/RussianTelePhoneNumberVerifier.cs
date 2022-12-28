namespace PaymentLogic.RequisiteParser.TelePhoneNumbersVerifier.Implementations;

public class RussianTelePhoneNumberVerifier : TelePhoneNumberVerifier
{
    protected override string telePhoneNumberRegexPattern => @"^((7|8|\+7)[\- ]?)(\(?\d{3}\)?[\- ]?)?[\d\- ]{7,10}$";
}