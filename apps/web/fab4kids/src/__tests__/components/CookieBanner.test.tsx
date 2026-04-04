import { render, screen, fireEvent } from '@testing-library/react';
import { CookieBanner } from '@/components/CookieBanner.tsx';

const CONSENT_KEY = 'fab4kids-cookie-consent';

describe('CookieBanner', () => {
  beforeEach(() => {
    vi.resetAllMocks();
    localStorage.clear();
  });

  it('renders banner when no consent is stored', () => {
    render(<CookieBanner />);

    expect(screen.getByRole('region', { name: /cookie consent/i })).toBeInTheDocument();
  });

  it('does not render banner when consent is already stored', () => {
    localStorage.setItem(CONSENT_KEY, 'accepted');

    render(<CookieBanner />);

    expect(screen.queryByRole('region', { name: /cookie consent/i })).not.toBeInTheDocument();
  });

  it('sets accepted consent in localStorage when Accept is clicked', () => {
    render(<CookieBanner />);

    fireEvent.click(screen.getByText(/accept all cookies/i));

    expect(localStorage.getItem(CONSENT_KEY)).toBe('accepted');
  });

  it('sets declined consent in localStorage when Decline is clicked', () => {
    render(<CookieBanner />);

    fireEvent.click(screen.getByText(/essential cookies only/i));

    expect(localStorage.getItem(CONSENT_KEY)).toBe('declined');
  });

  it('dispatches cookie-consent-accepted event when Accept is clicked', () => {
    const listener = vi.fn();
    window.addEventListener('cookie-consent-accepted', listener);
    render(<CookieBanner />);

    fireEvent.click(screen.getByText(/accept all cookies/i));

    expect(listener).toHaveBeenCalledOnce();
    window.removeEventListener('cookie-consent-accepted', listener);
  });
});
