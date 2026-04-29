import type { APIRoute } from 'astro';
import { addSubscriber } from '@/lib/newsletter.ts';
import { trackTrace, trackWarning, trackException, trackEvent } from '@/lib/telemetry';

export const prerender = false;

const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

export const POST: APIRoute = async ({ request }) => {
  let body: { email?: unknown; optIn?: unknown };
  try {
    body = await request.json() as { email?: unknown; optIn?: unknown };
  } catch {
    trackWarning('newsletter/invalid-body', { reason: 'parse-error' });

    return new Response(JSON.stringify({ error: 'Invalid request body' }), { status: 400 });
  }

  const { email, optIn } = body;

  if (typeof email !== 'string' || !EMAIL_PATTERN.test(email)) {
    trackWarning('newsletter/invalid-body', { reason: 'invalid-email' });

    return new Response(JSON.stringify({ error: 'Invalid email address' }), { status: 400 });
  }

  if (optIn !== true) {
    trackWarning('newsletter/invalid-body', { reason: 'no-consent' });

    return new Response(JSON.stringify({ error: 'Consent is required' }), { status: 400 });
  }

  try {
    trackTrace('newsletter/subscribe-start');
    addSubscriber(email);
    trackEvent('newsletter/subscribed');

    return new Response(JSON.stringify({ success: true }), { status: 200 });
  } catch (err) {
    const error = err instanceof Error ? err : new Error(String(err));
    trackException(error, { context: 'newsletter/subscribe-failed' });

    return new Response(JSON.stringify({ error: 'Internal server error' }), { status: 500 });
  }
};
