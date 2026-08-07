using System.Collections.Immutable;

namespace Demo.Models;

public class Invoice(
    InvoiceNumber number, string customerName, DateOnly invoicedOn,
    InvoiceStatus status, Currency currency)
{
    private Invoice(Invoice other)
        : this(other.Number, other.CustomerName, other.InvoicedOn, other.Status, other.Currency)
    {
        Id = other.Id;
        PublicId = other.PublicId;
        Lines = other.Lines;
    }

    private int Id { get; init; }
    public Guid PublicId { get; private init; } = Guid.NewGuid();

    public InvoiceNumber Number { get; init; } = number;

    public string CustomerName { get; init; } =
        !string.IsNullOrEmpty(customerName) ? customerName
        : throw new ArgumentException("Customer name cannot be null or empty.", nameof(customerName));

    public DateOnly InvoicedOn { get; init; } = invoicedOn;

    public InvoiceStatus Status { get; init; } = status;

    public Currency Currency { get; init; } = currency;

    public ImmutableList<InvoiceLine> Lines
    {
        get;
        init => field =
            value.All(line => line.UnitPrice.Currency == Currency) ? value
            : throw new ArgumentException("Mismatched currencies found in invoice lines.");
    } = ImmutableList<InvoiceLine>.Empty;

    public Invoice WithCustomerName(string customerName) =>
        new(this) { CustomerName = customerName };

    public Invoice WithInvoicedOn(DateOnly invoicedOn) =>
        new(this) { InvoicedOn = invoicedOn };

    public Invoice WithStatus(InvoiceStatus status) =>
        new(this) { Status = status };
    
    public Invoice WithLines(ImmutableList<InvoiceLine> lines) =>
        new(this) { Lines = lines };
    
    public Money Total =>
        Lines.Select(l => l.LineTotal).Aggregate(new Money(0, Currency), (a, b) => a + b);
}