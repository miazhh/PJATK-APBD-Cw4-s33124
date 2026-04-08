namespace LegacyRenewalApp;

public interface ICustomerRepository
{
    Customer GetById(int customerId);
}