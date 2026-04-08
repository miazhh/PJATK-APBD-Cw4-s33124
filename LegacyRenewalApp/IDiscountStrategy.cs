namespace LegacyRenewalApp;

public interface IDiscountStrategy
{
    (decimal Amount, string Note) Calculate(Customer customer, SubscriptionPlan subscriptionPlan, decimal baseAmount, int seatCount);
    
}