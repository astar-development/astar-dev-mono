import type { CartItem } from '@/types/index.ts';

export const CART_STORAGE_KEY = 'fab4kids-cart';
export const CART_UPDATED_EVENT = 'fab4kids:cart-updated';

export function readCart(): CartItem[] {
  try {
    const raw = localStorage.getItem(CART_STORAGE_KEY);
    if (!raw) return [];
    const parsed: unknown = JSON.parse(raw);
    if (!Array.isArray(parsed)) return [];

    return parsed as CartItem[];
  } catch {
    return [];
  }
}

export function writeCart(items: CartItem[]): void {
  try {
    localStorage.setItem(CART_STORAGE_KEY, JSON.stringify(items));
    window.dispatchEvent(new CustomEvent(CART_UPDATED_EVENT));
  } catch {
    return;
  }
}

export function addToCart(item: Omit<CartItem, 'quantity'>): void {
  const cart = readCart();
  const existing = cart.find((i) => i.productId === item.productId);
  const updated = existing
    ? cart.map((i) => i.productId === item.productId ? { ...i, quantity: i.quantity + 1 } : i)
    : [...cart, { ...item, quantity: 1 }];
  writeCart(updated);
}

export function removeFromCart(productId: string): void {
  writeCart(readCart().filter((i) => i.productId !== productId));
}

export function clearCart(): void {
  writeCart([]);
}

export function getTotalItems(cart: CartItem[]): number {
  return cart.reduce((sum, i) => sum + i.quantity, 0);
}

export function getTotalPrice(cart: CartItem[]): number {
  return cart.reduce((sum, i) => sum + i.price * i.quantity, 0);
}
