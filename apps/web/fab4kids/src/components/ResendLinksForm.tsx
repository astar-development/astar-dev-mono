import { useState } from 'react';

interface ResendLinksFormProps {
  sessionId: string;
  customerEmail: string;
}

type FormState = 'idle' | 'loading' | 'success' | 'error';

export function ResendLinksForm({ sessionId, customerEmail }: ResendLinksFormProps): React.JSX.Element {
  const [email, setEmail] = useState(customerEmail);
  const [state, setState] = useState<FormState>('idle');

  async function handleSubmit(e: React.FormEvent<HTMLFormElement>): Promise<void> {
    e.preventDefault();
    setState('loading');
    try {
      const res = await fetch('/api/resend-links', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ orderReference: sessionId, email }),
      });
      if (!res.ok) throw new Error('Failed');
      setState('success');
    } catch {
      setState('error');
    }
  }

  if (state === 'success') {
    return <p className="resend-success" role="status">New download links sent! Check your email.</p>;
  }

  return (
    <form onSubmit={handleSubmit} className="resend-form">
      <h3>Didn't receive your links?</h3>
      <label htmlFor="resend-email">Email address used at checkout</label>
      <input
        id="resend-email"
        type="email"
        value={email}
        onChange={(e) => setEmail(e.target.value)}
        required
        className="resend-input"
      />
      {state === 'error' && (
        <p className="resend-error" role="alert">
          Unable to resend links. Check your email address matches your order.
        </p>
      )}
      <button type="submit" disabled={state === 'loading'} className="btn-primary">
        {state === 'loading' ? 'Sending…' : 'Resend download links'}
      </button>
    </form>
  );
}
