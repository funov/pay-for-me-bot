namespace PaymentLogic.RequisiteParser.RequisiteMessageParser;

public class RequisiteMessageParser : IRequisiteMessageParser
{
    public PhoneNumberVerifier.PhoneNumberVerifier PhoneNumbersVerifier { get; }
    public BankingLinkVerifier.BankingLinkVerifier BankingLinkVerifier { get; }

    public RequisiteMessageParser(PhoneNumberVerifier.PhoneNumberVerifier phoneNumbersVerifier,
        BankingLinkVerifier.BankingLinkVerifier bankingLinkVerifier)
    {
        PhoneNumbersVerifier = phoneNumbersVerifier;
        BankingLinkVerifier = bankingLinkVerifier;
    }

    public bool IsRequisiteValid(string text)
    {
        text = text.Trim();
        var requisites = text.Split();

        if (requisites.Length == 2)
            return PhoneNumbersVerifier.IsPhoneNumberValid(requisites[0])
                   && BankingLinkVerifier.IsBankingLinkValid(requisites[1]);
        if (requisites.Length != 1)
            return false;

        var phoneAndLink = requisites[0].Split(" ");

        return phoneAndLink.Length switch
        {
            > 2 => false,
            1 => PhoneNumbersVerifier.IsPhoneNumberValid(phoneAndLink[0]),
            2 => PhoneNumbersVerifier.IsPhoneNumberValid(phoneAndLink[0])
                 && BankingLinkVerifier.IsBankingLinkValid(phoneAndLink[1]),
            _ => false
        };
    }
}