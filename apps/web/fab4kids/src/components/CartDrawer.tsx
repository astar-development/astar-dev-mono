import { useEffect, useState } from 'react';
import {
  CART_UPDATED_EVENT,
  clearCart,
  getTotalItems,
  getTotalPrice,
  readCart,
  removeFromCart,
} from '@/lib/cartStore.ts';
import type { CartItem } from '@/types/index.ts';

function formatPrice(pence: number): string {
  return `£${(pence / 100).toFixed(2)}`;
}

export function CartDrawer(): React.JSX.Element {
  const [cart, setCart] = useState<CartItem[]>([]);
  const [open, setOpen] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    setCart(readCart());

    function onCartUpdated(): void {
      setCart(readCart());
    }

    window.addEventListener(CART_UPDATED_EVENT, onCartUpdated);

    return () => window.removeEventListener(CART_UPDATED_EVENT, onCartUpdated);
  }, []);

  const totalItems = getTotalItems(cart);
  const totalPrice = getTotalPrice(cart);

  async function handleCheckout(): Promise<void> {
    setLoading(true);
    setError(null);
    try {
      const res = await fetch('/api/checkout', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          items: cart.map((i) => ({ stripePriceId: i.stripePriceId, quantity: i.quantity })),
        }),
      });
      if (!res.ok) throw new Error('Checkout failed');
      const { url } = await (res.json() as Promise<{ url: string }>);
      clearCart();
      window.location.href = url;
    } catch {
      setError('Unable to start checkout. Please try again.');
      setLoading(false);
    }
  }

  function handleRemove(productId: string): void {
    removeFromCart(productId);
  }

  return (
    <>
      <button
        type="button"
        className="cart-trigger"
        onClick={() => setOpen(true)}
        aria-label={`Open cart — ${totalItems} item${totalItems !== 1 ? 's' : ''}`}
      >
        🛒{totalItems > 0 && <span className="cart-badge" aria-hidden="true">{totalItems}</span>}
      </button>

      {open && <div className="cart-overlay" onClick={() => setOpen(false)} aria-hidden="true" />}

      <aside className={`cart-drawer${open ? ' open' : ''}`} aria-label="Shopping cart">
        <div className="cart-header">
          <h2>Your cart</h2>
          <button type="button" onClick={() => setOpen(false)} aria-label="Close cart">✕</button>
        </div>

        {cart.length === 0 ? (
          <p className="cart-empty">Your cart is empty.</p>
        ) : (
          <>
            <ul className="cart-items">
              {cart.map((item) => (
                <li key={item.productId} className="cart-item">
                  <div className="cart-item-info">
                    <span className="cart-item-title">{item.title}</span>
                    <span className="cart-item-price">{formatPrice(item.price * item.quantity)}</span>
                  </div>
                  <div className="cart-item-controls">
                    <span>Qty: {item.quantity}</span>
                    <button
                      type="button"
                      onClick={() => handleRemove(item.productId)}
                      aria-label={`Remove ${item.title} from cart`}
                      className="btn-remove"
                    >
                      Remove
                    </button>
                  </div>
                </li>
              ))}
            </ul>

            <div className="cart-footer">
              <p className="cart-total">Total: <strong>{formatPrice(totalPrice)}</strong></p>
              {error && <p className="cart-error" role="alert">{error}</p>}
              <button
                type="button"
                onClick={handleCheckout}
                disabled={loading}
                className="btn-checkout"
              >
                {loading ? 'Processing…' : 'Checkout'}
              </button>
            </div>
          </>
        )}
      </aside>
    </>
  );
}
