namespace Fab4Kids.Web.Cart;

/// <summary>Factory for <see cref="CartItem"/>.</summary>
public static class CartItemFactory
{
    public static CartItem Create(int productId, string? name, decimal price, int quantity, string? blobPath = null)
        => new(productId, string.IsNullOrWhiteSpace(name) ? "Untitled resource" : name.Trim(), price < 0 ? 0m : price, quantity < 1 ? 1 : quantity, blobPath?.Trim() ?? string.Empty);
}
