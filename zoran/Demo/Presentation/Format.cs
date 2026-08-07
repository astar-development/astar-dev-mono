using System.Globalization;
using Demo.Models;

namespace Demo.Presentation;

public static class Format
{
    static string Line => "------------------------------------------------------------";

    extension(Invoice invoice)
    {
        public string ToLabel() =>
            string.Join(Environment.NewLine, invoice.ToLabelLines());

        public IEnumerable<string> ToLabelLines() =>
        [
            $"{invoice.Number.ToLabel()} for [{invoice.CustomerName,-20}] " +
            $"on {invoice.InvoicedOn.ToLabel()}{invoice.Status.ToInvoiceLabel()} " +
            $"[ref. {invoice.PublicId}]",
            Line,
            ..invoice.Lines.Select(l => l.ToLabel()),
            Line,
            InvoiceLine.TotalToLabel(invoice.Total),
        ];
    }

    extension(InvoiceLine line)
    {
        public string ToLabel() =>
            $"   - {line.Description,-18}  x {line.Quantity,2}  @ {line.UnitPrice.Amount,8:0.00} {line.UnitPrice.Currency.Code} | {line.LineTotal.Amount,8:0.00} {line.UnitPrice.Currency.Code}";

        public IEnumerable<string> ToLabelLines() => [line.ToLabel()];
    }

    extension(InvoiceLine)
    {
        public static string TotalToLabel(Money amount) =>
            $"    {"TOTAL",41} | {amount.Amount,8:0.00} {amount.Currency.Code}";
    }

    extension(InvoiceStatus status)
    {
        public string ToInvoiceLabel() =>
            status == InvoiceStatus.Editing ? " (Editing)" : "";
    }

    extension(InvoiceNumber number)
    {
        public string ToLabel() =>
            $"{number.Year:D4}/{number.Sequence:D3}";
    }

    extension(DateOnly date)
    {
        public string ToLabel() =>
            date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }
}