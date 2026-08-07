using Demo.Models;
using ImmutableDomain.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Demo.Infrastructure;

public class DatabaseDbContext : DbContext
{
    public DatabaseDbContext(DbContextOptions<DatabaseDbContext> options) : base(options) { }

    public IImmutableEntityRepository<Invoice> Invoices =>
        Set<Invoice>().ToImmutableEntityRepository(this, "Lines");
    public IQueryable<Invoice> InvoicesQuery => Set<Invoice>().AsNoTracking();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<InvoiceLine>(entity =>
        {
            entity.HasKey("Id");
            entity.Property(e => e.Description).IsRequired();
            entity.Property(e => e.Quantity).IsRequired();

            entity.Ignore(e => e.LineTotal);
            entity.Ignore(e => e.UnitPrice);

            entity.Property<decimal>("UnitPriceAmount").IsRequired();
            entity.Property<string>("UnitPriceCurrency").IsRequired().HasMaxLength(3);
        });

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasKey("Id");
            entity.HasAlternateKey(invoice => invoice.PublicId);
            entity.HasIndex(index => index.PublicId).IsUnique().IsClustered(false);
            
            entity.Property(e => e.CustomerName).IsRequired();
            entity.Property(e => e.InvoicedOn).IsRequired();

            entity.Property(e => e.Number).HasConversion(
                v => $"{v.Year:D4}/{v.Sequence:D3}",
                v => new InvoiceNumber(
                    int.Parse(v.Substring(0, v.IndexOf('/'))),
                    int.Parse(v.Substring(v.IndexOf('/') + 1))));
            entity.Property(e => e.Number).IsRequired().HasMaxLength(8);
            entity.HasIndex(e => e.Number).IsUnique();

            entity.Property(e => e.Status)
                .HasConversion(v => v.ToString(), v => Enum.Parse<InvoiceStatus>(v))
                .HasMaxLength(Enum.GetValues<InvoiceStatus>().Max(s => s.ToString().Length));

            entity.Property(e => e.Currency).HasConversion(v => v.Code, v => new Currency(v));

            // entity.Ignore(e => e.Lines);
            entity.HasMany<InvoiceLine>(invoice => invoice.Lines)
                .WithOne()
                .OnDelete(DeleteBehavior.Cascade);
       });
    }
}