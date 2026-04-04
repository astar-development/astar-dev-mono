import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { NewsletterForm } from '@/components/NewsletterForm.tsx';

describe('NewsletterForm', () => {
  beforeEach(() => {
    vi.resetAllMocks();
    vi.stubGlobal('fetch', vi.fn<() => Promise<Response>>());
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('renders email input and opt-in checkbox', () => {
    render(<NewsletterForm />);

    expect(screen.getByLabelText(/email address/i)).toBeInTheDocument();
    expect(screen.getByRole('checkbox')).toBeInTheDocument();
  });

  it('does not submit if checkbox is unchecked', async () => {
    render(<NewsletterForm />);
    fireEvent.change(screen.getByLabelText(/email address/i), { target: { value: 'test@example.com' } });

    fireEvent.click(screen.getByRole('button', { name: /subscribe/i }));

    expect(vi.mocked(fetch)).not.toHaveBeenCalled();
    expect(await screen.findByRole('alert')).toBeInTheDocument();
  });

  it('shows success message after successful submission', async () => {
    vi.mocked(fetch).mockResolvedValueOnce(new Response('{}', { status: 200 }));
    render(<NewsletterForm />);
    fireEvent.change(screen.getByLabelText(/email address/i), { target: { value: 'test@example.com' } });
    fireEvent.click(screen.getByRole('checkbox'));

    fireEvent.click(screen.getByRole('button', { name: /subscribe/i }));

    await waitFor(() => expect(screen.getByRole('status')).toBeInTheDocument());
  });

  it('shows error message on failed submission', async () => {
    vi.mocked(fetch).mockResolvedValueOnce(new Response('{}', { status: 500 }));
    render(<NewsletterForm />);
    fireEvent.change(screen.getByLabelText(/email address/i), { target: { value: 'test@example.com' } });
    fireEvent.click(screen.getByRole('checkbox'));

    fireEvent.click(screen.getByRole('button', { name: /subscribe/i }));

    await waitFor(() => expect(screen.getByRole('alert')).toBeInTheDocument());
  });
});
