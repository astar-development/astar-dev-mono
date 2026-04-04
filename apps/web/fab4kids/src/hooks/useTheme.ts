import { useEffect, useState } from 'react';

export type Theme = 'light' | 'dark' | 'colourful';

const STORAGE_KEY = 'fab4kids-theme';
const VALID_THEMES: Theme[] = ['light', 'dark', 'colourful'];

function readStoredTheme(): Theme {
  try {
    const stored = localStorage.getItem(STORAGE_KEY);
    if (stored !== null && (VALID_THEMES as string[]).includes(stored)) {
      return stored as Theme;
    }
  } catch {
    // SecurityError in private browsing — fall through to OS preference
  }
  try {
    if (window.matchMedia('(prefers-color-scheme: dark)').matches) {
      return 'dark';
    }
  } catch {
    // matchMedia unavailable (SSR/test env without mock) — fall through
  }

  return 'light';
}

export function useTheme(): { theme: Theme; setTheme: (t: Theme) => void } {
  const [theme, setThemeState] = useState<Theme>(readStoredTheme);

  useEffect(() => {
    document.documentElement.setAttribute('data-theme', theme);
    try {
      localStorage.setItem(STORAGE_KEY, theme);
    } catch {
      // SecurityError in private browsing — ignore
    }
  }, [theme]);

  function setTheme(t: Theme): void {
    setThemeState(t);
  }

  return { theme, setTheme };
}
