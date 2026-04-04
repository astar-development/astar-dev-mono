import { renderHook, act } from '@testing-library/react';
import { useTheme } from '@/hooks/useTheme.ts';

const THEME_KEY = 'fab4kids-theme';

function mockMatchMedia(prefersDark: boolean): void {
  Object.defineProperty(window, 'matchMedia', {
    writable: true,
    configurable: true,
    value: vi.fn().mockImplementation((query: string) => ({
      matches: query === '(prefers-color-scheme: dark)' && prefersDark,
      addEventListener: vi.fn(),
      removeEventListener: vi.fn(),
    })),
  });
}

describe('useTheme', () => {
  beforeEach(() => {
    vi.resetAllMocks();
    localStorage.clear();
    document.documentElement.removeAttribute('data-theme');
    mockMatchMedia(false);
  });

  it('defaults to light when no preference is stored and no OS preference', () => {
    const { result } = renderHook(() => useTheme());

    expect(result.current.theme).toBe('light');
  });

  it('defaults to dark when OS prefers dark and nothing is stored', () => {
    mockMatchMedia(true);

    const { result } = renderHook(() => useTheme());

    expect(result.current.theme).toBe('dark');
  });

  it('loads the stored theme from localStorage on mount', () => {
    localStorage.setItem(THEME_KEY, 'colourful');

    const { result } = renderHook(() => useTheme());

    expect(result.current.theme).toBe('colourful');
  });

  it('persists the selected theme to localStorage on change', () => {
    const { result } = renderHook(() => useTheme());

    act(() => result.current.setTheme('dark'));

    expect(localStorage.getItem(THEME_KEY)).toBe('dark');
  });

  it('updates the data-theme attribute on html element when theme changes', () => {
    const { result } = renderHook(() => useTheme());

    act(() => result.current.setTheme('colourful'));

    expect(document.documentElement.getAttribute('data-theme')).toBe('colourful');
  });
});
