namespace LegacyRenewalApp;

public class SegmentDiscount : IDiscountStrategy
{
    public (decimal Amount, string Note) Calculate(Customer customer, SubscriptionPlan subscriptionPlan, decimal baseAmount,
        int seatCount)
    {
        if (customer.Segment == "Silver") return (baseAmount * 0.05m, "silver discount; ");
        if (customer.Segment == "Gold") return (baseAmount * 0.1m, "gold discount; ");
        if (customer.Segment == "Platinum") return (baseAmount * 0.15m, "platinum discount; ");
        if (customer.Segment == "Education" && subscriptionPlan.IsEducationEligible) return (baseAmount * 0.2m, "education discount; ");
        return (0, "");

    }
}