namespace PaymentLogic.RequisiteParser.RequisiteMessageParser;

public interface IRequisiteMessageParser
{
    PhoneNumberVerifier.PhoneNumberVerifier PhoneNumbersVerifier { get; }
    BankingLinkVerifier.BankingLinkVerifier BankingLinkVerifier { get; }

    bool IsRequisiteValid(string text);
}