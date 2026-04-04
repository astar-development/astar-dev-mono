import { useEffect, useState } from 'react';

const CONSENT_KEY = 'fab4kids-cookie-consent';
type ConsentValue = 'accepted' | 'declined';

function getStoredConsent(): ConsentValue | null {
  try {
    const stored = localStorage.getItem(CONSENT_KEY);
    if (stored === 'accepted' || stored === 'declined') return stored;

    return null;
  } catch {
    return null;
  }
}

function storeConsent(value: ConsentValue): void {
  try {
    localStorage.setItem(CONSENT_KEY, value);
  } catch {
    return;
  }
}

export function CookieBanner(): React.JSX.Element | null {
  const [visible, setVisible] = useState(false);

  useEffect(() => {
    if (getStoredConsent() === null) {
      setVisible(true);
    }
  }, []);

  function accept(): void {
    storeConsent('accepted');
    setVisible(false);
    window.dispatchEvent(new CustomEvent('cookie-consent-accepted'));
  }

  function decline(): void {
    storeConsent('declined');
    setVisible(false);
  }

  if (!visible) return null;

  return (
    <div className="cookie-banner" role="region" aria-label="Cookie consent">
      <p className="cookie-banner-text">
        We use cookies to improve your experience and process payments securely.
        Essential cookies are always active.{' '}
        <a href="/cookie-policy">Learn more</a>.
      </p>
      <div className="cookie-banner-actions">
        <button type="button" onClick={accept} className="btn-primary">
          Accept all cookies
        </button>
        <button type="button" onClick={decline} className="btn-secondary">
          Essential cookies only
        </button>
      </div>
    </div>
  );
}
