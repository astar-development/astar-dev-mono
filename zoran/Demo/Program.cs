using Demo.Infrastructure;
using Demo.Models;
using Demo.Presentation;
using Microsoft.EntityFrameworkCore;

var dbContextFactory = new DatabaseDbContextFactory();
using var dbContext = dbContextFactory.CreateDbContext();

await dbContext.Database.EnsureDeletedAsync();
await dbContext.Database.MigrateAsync();

var number = new InvoiceNumber(2026, 9);
var issuedOn = new DateOnly(2026, 1, 15);
var usd = new Currency("USD");

var invoice = new Invoice(number, "Big Joe", issuedOn, InvoiceStatus.Issued, usd)
    .WithLines(
    [
        new InvoiceLine("Thinking Hat", 2, new Money(19.99m, usd)),
        new InvoiceLine("Sleeping Glasses", 1, new Money(29.99m, usd))
    ]);

await dbContext.Invoices.AddImmutableAsync(invoice);
await dbContext.SaveChangesAsync();

var invoiceId = invoice.PublicId;
var loadedInvoice = await dbContext.Invoices.FindImmutableAsync(invoiceId) ?? throw new Exception("Not found.");

Console.WriteLine();
Console.WriteLine("Initial invoice:");
Console.WriteLine(loadedInvoice.ToLabel());

var cloak = new InvoiceLine("Invisibility Cloak", 1, new Money(99.99m, usd));
var updatedInvoice = loadedInvoice
    .WithCustomerName("Sleepy Sam")
    .WithLines(loadedInvoice.Lines.Add(cloak));

dbContext.Invoices.UpdateImmutable(updatedInvoice);
await dbContext.SaveChangesAsync();

var finalInvoice = await dbContext.Invoices.FindImmutableAsync(invoiceId) ?? throw new Exception("Not found.");
Console.WriteLine();
Console.WriteLine(new string('=', 100));
Console.WriteLine();
Console.WriteLine("Updated invoice (reloaded):");
Console.WriteLine(finalInvoice.ToLabel());

Console.WriteLine();
Console.WriteLine("Original invoice:");
Console.WriteLine(loadedInvoice.ToLabel());