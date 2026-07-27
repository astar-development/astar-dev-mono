window.astarClipboard = {
  isSupported: function () {
    return typeof navigator !== 'undefined' && !!navigator.clipboard;
  },
  copy: async function (text) {
    try {
      await navigator.clipboard.writeText(text);
      return true;
    } catch {
      return false;
    }
  },
};
