window.fab4kidsCookieConsent = {
  notifyAccepted: function () {
    window.dispatchEvent(new Event('cookie-consent-accepted'));
  },
};
