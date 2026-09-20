using Billing.Domain;

namespace Billing.Application;

public sealed record BillingLine(int MenuItemId, string ItemName, decimal UnitPrice, int Quantity, decimal GSTPercentage, decimal Total);
public sealed record BillingResult(IReadOnlyList<BillingLine> Items, decimal Subtotal, decimal Discount, decimal Tax, decimal GrandTotal);

public sealed class BillingCalculator
{
    public BillingResult Calculate(IEnumerable<(MenuItem Item, int Quantity)> selections, decimal discount, bool taxEnabled)
    {
        var lines = selections.Select(selection =>
        {
            if (selection.Quantity <= 0) throw new ArgumentOutOfRangeException(nameof(selection.Quantity));
            var total = decimal.Round(selection.Item.Price * selection.Quantity, 2);
            return new BillingLine(selection.Item.Id, selection.Item.Name, selection.Item.Price, selection.Quantity, selection.Item.GSTPercentage, total);
        }).ToList();

        if (lines.Count == 0) throw new InvalidOperationException("An order must contain at least one item.");
        if (discount < 0) throw new ArgumentOutOfRangeException(nameof(discount));

        var subtotal = lines.Sum(line => line.Total);
        var safeDiscount = decimal.Round(Math.Min(discount, subtotal), 2);
        var tax = taxEnabled ? decimal.Round(lines.Sum(line => (line.Total - safeDiscount * line.Total / subtotal) * line.GSTPercentage / 100), 2) : 0;
        return new BillingResult(lines, subtotal, safeDiscount, tax, decimal.Round(subtotal - safeDiscount + tax, 2));
    }
}
