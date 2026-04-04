import type { APIRoute } from 'astro';
import { addSubscriber } from '@/lib/newsletter.ts';

export const prerender = false;

const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

export const POST: APIRoute = async ({ request }) => {
  let body: { email?: unknown; optIn?: unknown };
  try {
    body = await request.json() as { email?: unknown; optIn?: unknown };
  } catch {
    return new Response(JSON.stringify({ error: 'Invalid request body' }), { status: 400 });
  }

  const { email, optIn } = body;

  if (typeof email !== 'string' || !EMAIL_PATTERN.test(email)) {
    return new Response(JSON.stringify({ error: 'Invalid email address' }), { status: 400 });
  }

  if (optIn !== true) {
    return new Response(JSON.stringify({ error: 'Consent is required' }), { status: 400 });
  }

  try {
    addSubscriber(email);

    return new Response(JSON.stringify({ success: true }), { status: 200 });
  } catch (err) {
    console.error('Newsletter subscription error', err);

    return new Response(JSON.stringify({ error: 'Internal server error' }), { status: 500 });
  }
};
