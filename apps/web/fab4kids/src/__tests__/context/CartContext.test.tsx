import { addToCart, clearCart, getTotalItems, getTotalPrice, readCart, removeFromCart, writeCart } from '@/lib/cartStore.ts';

const PRODUCT_A = { productId: 'p1', slug: 'year-3-maths', title: 'Year 3 Maths Pack', price: 299, stripePriceId: 'price_001' };
const PRODUCT_B = { productId: 'p2', slug: 'year-4-english', title: 'Year 4 English Pack', price: 349, stripePriceId: 'price_002' };

describe('cartStore', () => {
  beforeEach(() => {
    vi.resetAllMocks();
    localStorage.clear();
  });

  it('adds a product to the cart', () => {
    addToCart(PRODUCT_A);

    expect(readCart()).toHaveLength(1);
  });

  it('increments quantity when the same product is added twice', () => {
    addToCart(PRODUCT_A);
    addToCart(PRODUCT_A);

    const cart = readCart();
    expect(cart).toHaveLength(1);
    expect(cart[0]?.quantity).toBe(2);
  });

  it('removes a product from the cart', () => {
    addToCart(PRODUCT_A);

    removeFromCart(PRODUCT_A.productId);

    expect(readCart()).toHaveLength(0);
  });

  it('persists cart to localStorage on change', () => {
    addToCart(PRODUCT_A);

    expect(localStorage.getItem('fab4kids-cart')).toContain('p1');
  });

  it('restores cart from localStorage on read', () => {
    writeCart([{ ...PRODUCT_A, quantity: 2 }]);

    const cart = readCart();
    expect(cart[0]?.quantity).toBe(2);
  });

  it('clears all items from the cart', () => {
    addToCart(PRODUCT_A);
    addToCart(PRODUCT_B);

    clearCart();

    expect(readCart()).toHaveLength(0);
  });

  it('calculates total items correctly', () => {
    addToCart(PRODUCT_A);
    addToCart(PRODUCT_A);
    addToCart(PRODUCT_B);

    expect(getTotalItems(readCart())).toBe(3);
  });

  it('calculates total price correctly', () => {
    addToCart(PRODUCT_A);
    addToCart(PRODUCT_B);

    expect(getTotalPrice(readCart())).toBe(299 + 349);
  });
});
