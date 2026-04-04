import { render, screen, fireEvent } from '@testing-library/react';
import { ThemeSwitcher } from '@/components/ThemeSwitcher.tsx';
import * as useThemeModule from '@/hooks/useTheme.ts';

describe('ThemeSwitcher', () => {
  beforeEach(() => {
    vi.resetAllMocks();
  });

  it('renders three theme buttons', () => {
    vi.spyOn(useThemeModule, 'useTheme').mockReturnValue({ theme: 'light', setTheme: vi.fn() });

    render(<ThemeSwitcher />);

    expect(screen.getAllByRole('button')).toHaveLength(3);
  });

  it('marks the active theme button as pressed', () => {
    vi.spyOn(useThemeModule, 'useTheme').mockReturnValue({ theme: 'dark', setTheme: vi.fn() });

    render(<ThemeSwitcher />);

    const darkButton = screen.getByRole('button', { name: /dark/i });
    expect(darkButton).toHaveAttribute('aria-pressed', 'true');
  });

  it('calls setTheme with the correct value when a button is clicked', () => {
    const setTheme = vi.fn();
    vi.spyOn(useThemeModule, 'useTheme').mockReturnValue({ theme: 'light', setTheme });

    render(<ThemeSwitcher />);
    fireEvent.click(screen.getByRole('button', { name: /colourful/i }));

    expect(setTheme).toHaveBeenCalledWith('colourful');
  });
});
