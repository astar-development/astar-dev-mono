import type { APIRoute } from 'astro';
import Stripe from 'stripe';
import { trackTrace, trackWarning, trackException, trackEvent } from '@/lib/telemetry';

export const prerender = false;

interface CheckoutItem {
  stripePriceId: string;
  quantity: number;
}

export const POST: APIRoute = async ({ request }) => {
  let body: { items?: unknown };
  try {
    body = await request.json() as { items?: unknown };
  } catch {
    trackWarning('checkout/invalid-body', { reason: 'parse-error' });

    return new Response(JSON.stringify({ error: 'Invalid request body' }), { status: 400 });
  }

  const { items } = body;
  if (!Array.isArray(items) || items.length === 0) {
    trackWarning('checkout/invalid-body', { reason: 'empty-cart' });

    return new Response(JSON.stringify({ error: 'Cart is empty' }), { status: 400 });
  }

  const lineItems = (items as CheckoutItem[]).map((item) => ({
    price: item.stripePriceId,
    quantity: item.quantity,
  }));

  const stripe = new Stripe(import.meta.env.STRIPE_SECRET_KEY);
  const siteUrl = import.meta.env.PUBLIC_SITE_URL;

  try {
    trackTrace('checkout/session-start', { itemCount: String(lineItems.length) });

    const session = await stripe.checkout.sessions.create({
      mode: 'payment',
      line_items: lineItems,
      automatic_tax: { enabled: true },
      tax_id_collection: { enabled: false },
      success_url: `${siteUrl}/checkout/success?session_id={CHECKOUT_SESSION_ID}`,
      cancel_url: siteUrl,
      metadata: { source: 'fab4kids' },
    });

    trackEvent('checkout/session-created', { sessionId: session.id ?? 'unknown', itemCount: String(lineItems.length) });

    return new Response(JSON.stringify({ url: session.url }), { status: 200 });
  } catch (err) {
    const error = err instanceof Error ? err : new Error(String(err));
    trackException(error, { context: 'checkout/session-failed', itemCount: String(lineItems.length) });

    return new Response(JSON.stringify({ error: 'Unable to create checkout session' }), { status: 500 });
  }
};
