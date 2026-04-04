import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useReducer,
} from 'react';
import type { ReactNode } from 'react';
import type { CartItem } from '@/types/index.ts';

const STORAGE_KEY = 'fab4kids-cart';

type CartAction =
  | { type: 'ADD_ITEM'; item: Omit<CartItem, 'quantity'> }
  | { type: 'REMOVE_ITEM'; productId: string }
  | { type: 'CLEAR_CART' }
  | { type: 'HYDRATE'; items: CartItem[] };

function cartReducer(state: CartItem[], action: CartAction): CartItem[] {
  switch (action.type) {
    case 'ADD_ITEM': {
      const existing = state.find((i) => i.productId === action.item.productId);
      if (existing) {
        return state.map((i) =>
          i.productId === action.item.productId
            ? { ...i, quantity: i.quantity + 1 }
            : i,
        );
      }

      return [...state, { ...action.item, quantity: 1 }];
    }
    case 'REMOVE_ITEM':
      return state.filter((i) => i.productId !== action.productId);
    case 'CLEAR_CART':
      return [];
    case 'HYDRATE':
      return action.items;
  }
}

interface CartContextValue {
  cart: CartItem[];
  addItem: (item: Omit<CartItem, 'quantity'>) => void;
  removeItem: (productId: string) => void;
  clearCart: () => void;
  totalItems: number;
  totalPrice: number;
}

const CartContext = createContext<CartContextValue | null>(null);

function readCartFromStorage(): CartItem[] {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) return [];
    const parsed: unknown = JSON.parse(raw);
    if (!Array.isArray(parsed)) return [];

    return parsed as CartItem[];
  } catch {
    return [];
  }
}

export function CartProvider({ children }: { children: ReactNode }): React.JSX.Element {
  const [cart, dispatch] = useReducer(cartReducer, []);

  useEffect(() => {
    dispatch({ type: 'HYDRATE', items: readCartFromStorage() });
  }, []);

  useEffect(() => {
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(cart));
    } catch {
      return;
    }
  }, [cart]);

  const addItem = useCallback((item: Omit<CartItem, 'quantity'>): void => {
    dispatch({ type: 'ADD_ITEM', item });
  }, []);

  const removeItem = useCallback((productId: string): void => {
    dispatch({ type: 'REMOVE_ITEM', productId });
  }, []);

  const clearCart = useCallback((): void => {
    dispatch({ type: 'CLEAR_CART' });
  }, []);

  const totalItems = useMemo(
    () => cart.reduce((sum, i) => sum + i.quantity, 0),
    [cart],
  );

  const totalPrice = useMemo(
    () => cart.reduce((sum, i) => sum + i.price * i.quantity, 0),
    [cart],
  );

  const value = useMemo<CartContextValue>(
    () => ({ cart, addItem, removeItem, clearCart, totalItems, totalPrice }),
    [cart, addItem, removeItem, clearCart, totalItems, totalPrice],
  );

  return <CartContext.Provider value={value}>{children}</CartContext.Provider>;
}

export function useCart(): CartContextValue {
  const ctx = useContext(CartContext);
  if (!ctx) {
    throw new Error('useCart must be used within CartProvider');
  }

  return ctx;
}
