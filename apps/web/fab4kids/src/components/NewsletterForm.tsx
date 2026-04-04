import { useState } from 'react';

type FormState = 'idle' | 'loading' | 'success' | 'error';

const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

export function NewsletterForm(): React.JSX.Element {
  const [email, setEmail] = useState('');
  const [optIn, setOptIn] = useState(false);
  const [state, setState] = useState<FormState>('idle');
  const [validationError, setValidationError] = useState<string | null>(null);

  async function handleSubmit(e: React.FormEvent<HTMLFormElement>): Promise<void> {
    e.preventDefault();
    setValidationError(null);

    if (!EMAIL_PATTERN.test(email)) {
      setValidationError('Please enter a valid email address.');

      return;
    }
    if (!optIn) {
      setValidationError('Please check the consent box to subscribe.');

      return;
    }

    setState('loading');
    try {
      const res = await fetch('/api/newsletter', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, optIn }),
      });
      if (!res.ok) throw new Error('Subscription failed');
      setState('success');
    } catch {
      setState('error');
    }
  }

  if (state === 'success') {
    return (
      <p className="newsletter-success" role="status">
        Thanks! We'll be in touch with the latest resources.
      </p>
    );
  }

  return (
    <form onSubmit={handleSubmit} className="newsletter-form" noValidate>
      <div className="newsletter-field">
        <label htmlFor="newsletter-email" className="newsletter-label">Email address</label>
        <input
          id="newsletter-email"
          type="email"
          name="email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          placeholder="you@example.com"
          aria-label="Email address"
          aria-required="true"
          required
          className="newsletter-input"
        />
      </div>

      <div className="newsletter-optin">
        <input
          id="newsletter-optin"
          type="checkbox"
          checked={optIn}
          onChange={(e) => setOptIn(e.target.checked)}
          aria-required="true"
        />
        <label htmlFor="newsletter-optin">
          I agree to receive educational resource news and updates from fab4kids.
          See our <a href="/privacy-policy">Privacy Policy</a>.
        </label>
      </div>

      {validationError && (
        <p className="newsletter-error" role="alert">{validationError}</p>
      )}
      {state === 'error' && (
        <p className="newsletter-error" role="alert">
          Something went wrong. Please try again.
        </p>
      )}

      <button
        type="submit"
        disabled={state === 'loading'}
        className="btn-primary newsletter-submit"
      >
        {state === 'loading' ? 'Subscribing…' : 'Subscribe'}
      </button>
    </form>
  );
}
