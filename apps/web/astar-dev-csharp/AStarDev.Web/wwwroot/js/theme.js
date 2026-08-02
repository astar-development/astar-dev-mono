window.astarTheme = {
  validThemes: ['dark', 'light', 'metal', 'polished'],
  applyThemeClass: function (theme) {
    document.documentElement.className = 'theme-' + theme;
  },
  applyStoredTheme: function () {
    var stored;
    try { stored = localStorage.getItem('theme'); } catch (_) { }
    var theme = window.astarTheme.validThemes.indexOf(stored) !== -1 ? stored : 'dark';
    window.astarTheme.applyThemeClass(theme);
  },
};

if (window.Blazor && typeof Blazor.addEventListener === 'function') {
  Blazor.addEventListener('enhancedload', window.astarTheme.applyStoredTheme);
}
