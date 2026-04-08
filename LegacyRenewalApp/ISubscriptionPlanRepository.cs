namespace LegacyRenewalApp;

public interface ISubscriptionPlanRepository
{
    SubscriptionPlan GetByCode(string code);
}