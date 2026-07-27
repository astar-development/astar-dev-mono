using Blazored.LocalStorage;

namespace Fab4Kids.Web.Cart;

/// <summary>
/// Tracks the visitor's basket for the current circuit and persists it to browser
/// local storage, mirroring the previous Astro site's <c>cartStore.ts</c> behaviour.
/// </summary>
public sealed class CartState(ILocalStorageService localStorage)
{
    private const string StorageKey = "fab4kids-cart";

    public IReadOnlyList<CartItem> Items { get; private set; } = [];

    public int TotalItems => Items.Sum(item => item.Quantity);

    public decimal TotalPrice => Items.Sum(item => item.Price * item.Quantity);

    public event Action? OnChange;

    public async Task InitializeAsync()
    {
        var stored = await localStorage.GetItemAsync<IReadOnlyList<CartItem>>(StorageKey);
        Items = stored ?? [];
    }

    public async Task AddItemAsync(int productId, string name, decimal price)
    {
        var existing = Items.FirstOrDefault(item => item.ProductId == productId);
        Items = existing is null
            ? [.. Items, CartItemFactory.Create(productId, name, price, 1)]
            : [.. Items.Select(item => item.ProductId == productId ? item with { Quantity = item.Quantity + 1 } : item)];

        await PersistAsync();
    }

    public async Task RemoveItemAsync(int productId)
    {
        Items = [.. Items.Where(item => item.ProductId != productId)];

        await PersistAsync();
    }

    private async Task PersistAsync()
    {
        await localStorage.SetItemAsync(StorageKey, Items);
        OnChange?.Invoke();
    }
}
