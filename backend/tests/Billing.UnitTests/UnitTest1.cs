using Billing.Application;
using Billing.Domain;

namespace Billing.UnitTests;

public sealed class BillingCalculatorTests
{
    [Fact]
    public void Calculates_subtotal_discount_tax_and_total()
    {
        var calculator = new BillingCalculator();
        var item = new MenuItem { Id = 1, Name = "Burger", Price = 100, GSTPercentage = 5 };

        var result = calculator.Calculate([(item, 2)], 20, true);

        Assert.Equal(200, result.Subtotal);
        Assert.Equal(20, result.Discount);
        Assert.Equal(9, result.Tax);
        Assert.Equal(189, result.GrandTotal);
    }

    [Fact]
    public void Rejects_empty_orders()
    {
        var calculator = new BillingCalculator();

        Assert.Throws<InvalidOperationException>(() => calculator.Calculate([], 0, true));
    }
}
