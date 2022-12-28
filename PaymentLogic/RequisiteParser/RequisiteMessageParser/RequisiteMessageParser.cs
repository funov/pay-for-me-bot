using PaymentLogic.RequisiteParser.BankingLinkVerifier.Implementations;
using PaymentLogic.RequisiteParser.TelePhoneNumbersVerifier;
using PaymentLogic.RequisiteParser.TelePhoneNumbersVerifier.Implementations;

namespace PaymentLogic.RequisiteParser.RequisiteMessagePaesser;

public class RequisiteMessageParser
{
    // TODO мб подумать насчет выноса этих полей
    
    public static bool IsRequisiteValid(string text, TelePhoneNumberVerifier telePhoneNumbersVerifier,
        BankingLinkVerifier.BankingLinkVerifier bankingLinkVerifier)
    {
        text = text.Trim();
        var requisites = text.Split();

        if (requisites.Length == 2)
            return
                telePhoneNumbersVerifier.IsTelePhoneNumberValid(requisites[0])
                && bankingLinkVerifier.IsBankingLinkValid(requisites[1]);
        if (requisites.Length != 1)
            return false;

        var phoneAndLink = requisites[0].Split(" ");

        return phoneAndLink.Length switch
        {
            > 2 => false,
            1 => telePhoneNumbersVerifier.IsTelePhoneNumberValid(phoneAndLink[0]),
            2 => telePhoneNumbersVerifier.IsTelePhoneNumberValid(phoneAndLink[0])
                 && bankingLinkVerifier.IsBankingLinkValid(phoneAndLink[1]),
            _ => false
        };
    }
}