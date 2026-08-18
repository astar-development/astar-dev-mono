window.astarTheme = {
  validThemes: ['dark', 'light', 'metal', 'polished'],
  applyThemeClass: function (theme) {
    document.documentElement.className = 'theme-' + theme;
  },
  applyStoredTheme: function () {
    let stored;
    try { stored = localStorage.getItem('theme'); } catch (_) { }
    let theme = window.astarTheme.validThemes.includes(stored) ? stored : 'dark';
    window.astarTheme.applyThemeClass(theme);
  },
};

if (window.Blazor && typeof Blazor.addEventListener === 'function') {
  Blazor.addEventListener('enhancedload', window.astarTheme.applyStoredTheme);
}
