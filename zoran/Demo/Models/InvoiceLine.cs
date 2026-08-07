namespace Demo.Models;

public record InvoiceLine(string Description, int Quantity, Money UnitPrice)
{
    private int Id { get; init; }
    public Money LineTotal => Quantity * UnitPrice;

    private decimal UnitPriceAmount { get; init; } = UnitPrice.Amount;
    private string UnitPriceCurrency { get; init; } = UnitPrice.Currency.Code;

    private InvoiceLine(string Description, int Quantity, decimal unitPriceAmount, string unitPriceCurrency)
        : this(Description, Quantity, new Money(unitPriceAmount, new Currency(unitPriceCurrency)))
    {
    }
}