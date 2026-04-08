namespace LegacyRenewalApp;

public interface ILegacyBillingGateway
{
    void SaveInvoice(RenewalInvoice invoice);
    void SendEmail(string email, string subject, string body);
}