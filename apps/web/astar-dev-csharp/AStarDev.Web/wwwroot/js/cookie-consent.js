window.astarCookieConsent = {
  notifyAccepted: function () {
    window.dispatchEvent(new Event('cookie-consent-accepted'));
  },
};
