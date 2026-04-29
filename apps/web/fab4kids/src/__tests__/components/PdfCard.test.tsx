import { act, render, screen, fireEvent } from '@testing-library/react';
import { PdfCard } from '@/components/PdfCard.tsx';
import { readCart } from '@/lib/cartStore.ts';

const FILE = { id: 7, name: 'Fractions Worksheet', url: 'pdfs/fractions.pdf', price: 1.23 };

describe('PdfCard', () => {
  beforeEach(() => {
    localStorage.clear();
  });

  it('when_rendered_then_displays_file_name', () => {
    render(<PdfCard file={FILE} />);

    expect(screen.getByRole('heading', { name: 'Fractions Worksheet' })).toBeInTheDocument();
  });

  it('when_rendered_then_view_link_points_to_file_url', () => {
    render(<PdfCard file={FILE} />);

    const link = screen.getByRole('link', { name: /view fractions worksheet/i });

    expect(link).toHaveAttribute('href', 'pdfs/fractions.pdf');
  });

  it('when_rendered_then_view_link_opens_in_new_tab', () => {
    render(<PdfCard file={FILE} />);

    const link = screen.getByRole('link', { name: /view fractions worksheet/i });

    expect(link).toHaveAttribute('target', '_blank');
    expect(link).toHaveAttribute('rel', 'noopener noreferrer');
  });

  it('when_rendered_then_displays_price_in_pounds', () => {
    render(<PdfCard file={FILE} />);

    expect(screen.getByText('£1.23')).toBeInTheDocument();
  });

  it('when_rendered_then_add_to_basket_button_present', () => {
    render(<PdfCard file={FILE} />);

    expect(screen.getByRole('button', { name: /add fractions worksheet to basket/i })).toBeInTheDocument();
  });

  it('when_add_to_basket_clicked_then_file_added_to_cart', () => {
    render(<PdfCard file={FILE} />);

    fireEvent.click(screen.getByRole('button', { name: /add.*to basket/i }));

    const cart = readCart();
    expect(cart).toHaveLength(1);
    expect(cart[0].title).toBe('Fractions Worksheet');
    expect(cart[0].productId).toBe('7');
    expect(cart[0].price).toBe(123);
  });

  it('when_add_to_basket_clicked_twice_then_quantity_increments', () => {
    render(<PdfCard file={FILE} />);

    fireEvent.click(screen.getByRole('button', { name: /add.*to basket/i }));
    fireEvent.click(screen.getByRole('button'));

    expect(readCart()[0].quantity).toBe(2);
  });

  it('when_add_to_basket_clicked_then_button_shows_added_confirmation', () => {
    render(<PdfCard file={FILE} />);

    fireEvent.click(screen.getByRole('button'));

    expect(screen.getByRole('button')).toHaveTextContent('Added ✓');
  });

  it('when_confirmation_timeout_elapses_then_button_reverts_to_add_to_basket', () => {
    vi.useFakeTimers();
    render(<PdfCard file={FILE} />);

    fireEvent.click(screen.getByRole('button'));
    act(() => { vi.advanceTimersByTime(1600); });

    expect(screen.getByRole('button')).toHaveTextContent('Add to basket');
    vi.useRealTimers();
  });
});
