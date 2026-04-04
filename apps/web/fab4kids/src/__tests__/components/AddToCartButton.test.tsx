import { act, render, screen, fireEvent } from '@testing-library/react';
import { AddToCartButton } from '@/components/AddToCartButton.tsx';
import { readCart } from '@/lib/cartStore.ts';

const PROPS = {
  productId: 'p1',
  slug: 'year-3-maths',
  title: 'Year 3 Maths Pack',
  price: 299,
  stripePriceId: 'price_001',
};

describe('AddToCartButton', () => {
  beforeEach(() => {
    vi.resetAllMocks();
    localStorage.clear();
  });

  it('renders Add to cart button', () => {
    render(<AddToCartButton {...PROPS} />);

    expect(screen.getByRole('button', { name: /add.*to cart/i })).toBeInTheDocument();
  });

  it('adds item to cart store when clicked', () => {
    render(<AddToCartButton {...PROPS} />);

    fireEvent.click(screen.getByRole('button'));

    expect(readCart()).toHaveLength(1);
  });

  it('shows Added text briefly after click then reverts', () => {
    vi.useFakeTimers();
    render(<AddToCartButton {...PROPS} />);

    fireEvent.click(screen.getByRole('button'));
    expect(screen.getByRole('button')).toHaveTextContent('Added ✓');

    act(() => { vi.advanceTimersByTime(1600); });

    expect(screen.getByRole('button')).toHaveTextContent('Add to cart');
    vi.useRealTimers();
  });
});
