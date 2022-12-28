namespace PaymentLogic.RequisiteParser.BankingLinkVerifier.Implementations;

public class TinkoffLinkVerifier : BankingLinkVerifier
{
    protected override string BankingLinkRegexPattern => @"https://www.tinkoff.ru/rm/[a-z]+.[a-z]+[0-9]+/[a-zA-z0-9]+";
}