namespace Fab4Kids.Web.Cart;

/// <summary>A single line in the visitor's shopping basket.</summary>
public sealed record CartItem(int ProductId, string Name, decimal Price, int Quantity);
