namespace Demo.Models;

public record InvoiceNumber(int Year, int Sequence)
{
    public override string ToString() => $"{Year:D4}/{Sequence:D3}";
}