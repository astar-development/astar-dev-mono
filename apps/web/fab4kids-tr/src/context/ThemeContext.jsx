import { createContext, useContext, useState, useEffect } from 'react';

const ThemeContext = createContext();

export const useTheme = () => {
  const context = useContext(ThemeContext);
  if (!context) {
    throw new Error('useTheme must be used within ThemeProvider');
  }
  return context;
};

export const ThemeProvider = ({ children }) => {
  const [theme, setTheme] = useState('rainbow-spots'); // 'rainbow-spots' or 'rainbow-gradient'

  const toggleTheme = () => {
    setTheme(prev => prev === 'rainbow-spots' ? 'rainbow-gradient' : 'rainbow-spots');
  };

  useEffect(() => {
    document.body.className = theme;

    if (theme === 'rainbow-spots') {
      const randomPercent = (min, max) => `${Math.floor(Math.random() * (max - min + 1) + min)}%`;

      document.body.style.setProperty('--arc1-top', randomPercent(2, 25));
      document.body.style.setProperty('--arc1-left', randomPercent(2, 25));

      document.body.style.setProperty('--arc2-top', randomPercent(5, 40));
      document.body.style.setProperty('--arc2-right', randomPercent(2, 22));

      document.body.style.setProperty('--arc3-bottom', randomPercent(5, 30));
      document.body.style.setProperty('--arc3-left', randomPercent(10, 40));

      document.body.style.setProperty('--arc4-bottom', randomPercent(0, 20));
      document.body.style.setProperty('--arc4-right', randomPercent(8, 35));
    }
  }, [theme]);

  return (
    <ThemeContext.Provider value={{ theme, toggleTheme }}>
      {children}
    </ThemeContext.Provider>
  );
};
