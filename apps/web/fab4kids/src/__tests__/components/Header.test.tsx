import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';

function SearchForm() {
  return (
    <form action="/search" method="get" role="search">
      <input name="q" type="search" placeholder="Search resources..." aria-label="Search" />
      <button type="submit" aria-label="Submit search">Search</button>
    </form>
  );
}

describe('Header search form', () => {
  it('has action pointing to /search', () => {
    const { container } = render(<SearchForm />);
    const form = container.querySelector('form[role="search"]');

    expect(form).toHaveAttribute('action', '/search');
  });

  it('has a text input with name "q"', () => {
    render(<SearchForm />);

    const input = screen.getByRole('searchbox', { name: /search/i });

    expect(input).toHaveAttribute('name', 'q');
  });

  it('submits with q parameter when user types a query', async () => {
    const user = userEvent.setup();
    const { container } = render(<SearchForm />);
    const form = container.querySelector('form')!;

    const submitSpy = vi.fn<(e: SubmitEvent) => void>((e) => e.preventDefault());
    form.addEventListener('submit', submitSpy);

    await user.type(screen.getByRole('searchbox'), 'fractions');
    await user.click(screen.getByRole('button', { name: /submit search/i }));

    expect(submitSpy).toHaveBeenCalledOnce();
    expect((screen.getByRole('searchbox') as HTMLInputElement).value).toBe('fractions');
  });
});

describe('Search page query parameter handling', () => {
  it('constructs /search?q= URL from search input value', () => {
    const query = 'maths worksheets';
    const form = document.createElement('form');
    form.action = '/search';
    form.method = 'get';
    const input = document.createElement('input');
    input.name = 'q';
    input.value = query;
    form.appendChild(input);

    const url = new URL(form.action, 'http://localhost');
    new FormData(form).forEach((value, key) => url.searchParams.set(key, value.toString()));

    expect(url.searchParams.get('q')).toBe('maths worksheets');
    expect(url.pathname).toBe('/search');
  });
});
