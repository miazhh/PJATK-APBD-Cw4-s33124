using System;
using System.Collections.Generic;

namespace LegacyRenewalApp
{
    public class SubscriptionRenewalService
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly ISubscriptionPlanRepository _subscriptionPlanRepository;
        private readonly ILegacyBillingGateway _legacyBillingGateway;
        
        private readonly List<IDiscountStrategy> _discountStrategies;

        public SubscriptionRenewalService() : this(new CustomerRepository(), new SubscriptionPlanRepository(), new LegacyBillingGatewayAdapter())
        {
            
        }

        public SubscriptionRenewalService(ICustomerRepository customerRepository,
            ISubscriptionPlanRepository subscriptionPlanRepository, ILegacyBillingGateway legacyBillingGateway)
        {
            _customerRepository = customerRepository;
            _subscriptionPlanRepository = subscriptionPlanRepository;
            _legacyBillingGateway = legacyBillingGateway;
            _discountStrategies = new List<IDiscountStrategy>{new EmployeeDiscount(), new SegmentDiscount(), new TeamSizeDiscount()};
        }
        
        public RenewalInvoice CreateRenewalInvoice(
            int customerId,
            string planCode,
            int seatCount,
            string paymentMethod,
            bool includePremiumSupport,
            bool useLoyaltyPoints)
        {
            if (customerId <= 0)
            {
                throw new ArgumentException("Customer id must be positive");
            }

            if (string.IsNullOrWhiteSpace(planCode))
            {
                throw new ArgumentException("Plan code is required");
            }

            if (seatCount <= 0)
            {
                throw new ArgumentException("Seat count must be positive");
            }

            if (string.IsNullOrWhiteSpace(paymentMethod))
            {
                throw new ArgumentException("Payment method is required");
            }

            string normalizedPlanCode = planCode.Trim().ToUpperInvariant();
            string normalizedPaymentMethod = paymentMethod.Trim().ToUpperInvariant();

            var customer = _customerRepository.GetById(customerId);
            var plan = _subscriptionPlanRepository.GetByCode(normalizedPlanCode);

            if (!customer.IsActive)
            {
                throw new InvalidOperationException("Inactive customers cannot renew subscriptions");
            }

            decimal baseAmount = (plan.MonthlyPricePerSeat * seatCount * 12m) + plan.SetupFee;
            decimal discountAmount = 0m;
            string notes = string.Empty;

            foreach (var strategy in _discountStrategies)
            {
                var(amount, note) = strategy.Calculate(customer, plan, baseAmount, seatCount);
                discountAmount += amount;
                notes += note;
            }

            if (useLoyaltyPoints && customer.LoyaltyPoints > 0)
            {
                int pointsToUse = customer.LoyaltyPoints > 200 ? 200 : customer.LoyaltyPoints;
                discountAmount += pointsToUse;
                notes += $"loyalty points used: {pointsToUse}; ";
            }

            decimal subtotalAfterDiscount = baseAmount - discountAmount;
            if (subtotalAfterDiscount < 300m)
            {
                subtotalAfterDiscount = 300m;
                notes += "minimum discounted subtotal applied; ";
            }

            decimal supportFee = CalculateSupportFee(includePremiumSupport, normalizedPlanCode);
            if (includePremiumSupport) notes += "premium support included; ";

            decimal paymentFee = CalculatePaymentFee(normalizedPaymentMethod, subtotalAfterDiscount + supportFee);
            notes += GetPaymentNote(normalizedPaymentMethod);

            decimal taxRate = GetTaxRate(customer.Country);

            decimal taxBase = subtotalAfterDiscount + supportFee + paymentFee;
            decimal taxAmount = taxBase * taxRate;
            decimal finalAmount = taxBase + taxAmount;

            if (finalAmount < 500m)
            {
                finalAmount = 500m;
                notes += "minimum invoice amount applied; ";
            }

            var invoice = new RenewalInvoice
            {
                InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{customerId}-{normalizedPlanCode}",
                CustomerName = customer.FullName,
                PlanCode = normalizedPlanCode,
                PaymentMethod = normalizedPaymentMethod,
                SeatCount = seatCount,
                BaseAmount = Math.Round(baseAmount, 2, MidpointRounding.AwayFromZero),
                DiscountAmount = Math.Round(discountAmount, 2, MidpointRounding.AwayFromZero),
                SupportFee = Math.Round(supportFee, 2, MidpointRounding.AwayFromZero),
                PaymentFee = Math.Round(paymentFee, 2, MidpointRounding.AwayFromZero),
                TaxAmount = Math.Round(taxAmount, 2, MidpointRounding.AwayFromZero),
                FinalAmount = Math.Round(finalAmount, 2, MidpointRounding.AwayFromZero),
                Notes = notes.Trim(),
                GeneratedAt = DateTime.UtcNow
            };

            _legacyBillingGateway.SaveInvoice(invoice);

            if (!string.IsNullOrWhiteSpace(customer.Email))
            {
                string subject = "Subscription renewal invoice";
                string body =
                    $"Hello {customer.FullName}, your renewal for plan {normalizedPlanCode} " +
                    $"has been prepared. Final amount: {invoice.FinalAmount:F2}.";

                _legacyBillingGateway.SendEmail(customer.Email, subject, body);
            }

            return invoice;
        }

        private decimal CalculateSupportFee(bool includePremiumSupport, string planCode)
        {
            if (!includePremiumSupport) return 0m;

            return planCode switch
            {
                "START" => 250m,
                "PRO" => 400m,
                "ENTERPRISE" => 700m,
                _ => 0m
            };
        }

        private decimal CalculatePaymentFee(string paymentMethod, decimal subtotal)
        {
            return paymentMethod switch
            {
                "CARD" => subtotal * 0.02m,
                "BANK_TRANSFER" => subtotal * 0.01m,
                "PAYPAL" => subtotal * 0.035m,
                "INVOICE" => 0m,
                _ => throw new ArgumentException("Unsupported payment method")
            };
        }

        private string GetPaymentNote(string paymentMethod)
        {
            return paymentMethod switch
            {
                "CARD" => "card payment fee; ",
                "BANK_TRANSFER" => "bank transfer fee; ",
                "PAYPAL" => "paypal fee; ",
                "INVOICE" => "invoice payment; ",
                _ => string.Empty
            };
        }

        private decimal GetTaxRate(string country)
        {
            return country switch
            {
                "Poland" => 0.23m,
                "Germany" => 0.19m,
                "Czech Republic" => 0.21m,
                "Norway" => 0.25m,
                _ => 0.2m
            };
        }
    }
}
