namespace LegacyRenewalApp;

public class EmployeeDiscount : IDiscountStrategy
{
    public (decimal Amount, string Note) Calculate(Customer customer, SubscriptionPlan subscriptionPlan, decimal baseAmount,
        int seatCount)
    {
        if(customer.YearsWithCompany >= 5) return (baseAmount * 0.07m, "long-term loyalty discount");
        if(customer.YearsWithCompany >= 2) return (baseAmount * 0.03m, "basic loyalty discount");
        return (0, "");
    }
}