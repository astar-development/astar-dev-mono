namespace Demo.Models;

public record struct Money(decimal Amount, Currency Currency);

public static class MoneyArithmetic
{
    extension(Money)
    {
        public static Money operator *(Money money, decimal factor) =>
            new Money(money.Amount * factor, money.Currency);
        
        public static Money operator *(decimal factor, Money money) =>
            money * factor;

        public static Money operator +(Money left, Money right) =>
            left.Currency == right.Currency ? new Money(left.Amount + right.Amount, left.Currency)
            : throw new InvalidOperationException("Cannot add Money values with different currencies.");
    }
}