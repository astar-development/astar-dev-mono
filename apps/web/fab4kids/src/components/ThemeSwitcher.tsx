import React from 'react';
import type { Theme } from '@/hooks/useTheme';
import { useTheme } from '@/hooks/useTheme';

interface ThemeOption {
  value: Theme;
  label: string;
  icon: string;
}

const THEME_OPTIONS: ThemeOption[] = [
  { value: 'light', label: 'Light', icon: '🌤' },
  { value: 'dark', label: 'Dark', icon: '🌙' },
  { value: 'colourful', label: 'Colourful', icon: '🎨' },
];

export function ThemeSwitcher(): React.JSX.Element {
  const { theme, setTheme } = useTheme();

  return (
    <div className="theme-switcher" role="group" aria-label="Choose colour theme">
      {THEME_OPTIONS.map(({ value, label, icon }) => (
        <button
          key={value}
          type="button"
          className={`theme-switcher__btn${theme === value ? ' theme-switcher__btn--active' : ''}`}
          aria-pressed={theme === value}
          onClick={() => setTheme(value)}
        >
          <span aria-hidden="true">{icon}</span>
          <span className="theme-switcher__label">{label}</span>
        </button>
      ))}
    </div>
  );
}
