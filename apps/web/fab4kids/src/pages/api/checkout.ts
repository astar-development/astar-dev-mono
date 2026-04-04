import type { APIRoute } from 'astro';
import Stripe from 'stripe';

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
    return new Response(JSON.stringify({ error: 'Invalid request body' }), { status: 400 });
  }

  const { items } = body;
  if (!Array.isArray(items) || items.length === 0) {
    return new Response(JSON.stringify({ error: 'Cart is empty' }), { status: 400 });
  }

  const lineItems = (items as CheckoutItem[]).map((item) => ({
    price: item.stripePriceId,
    quantity: item.quantity,
  }));

  const stripe = new Stripe(import.meta.env.STRIPE_SECRET_KEY);
  const siteUrl = import.meta.env.PUBLIC_SITE_URL;

  try {
    const session = await stripe.checkout.sessions.create({
      mode: 'payment',
      line_items: lineItems,
      automatic_tax: { enabled: true },
      tax_id_collection: { enabled: false },
      success_url: `${siteUrl}/checkout/success?session_id={CHECKOUT_SESSION_ID}`,
      cancel_url: siteUrl,
      metadata: { source: 'fab4kids' },
    });

    return new Response(JSON.stringify({ url: session.url }), { status: 200 });
  } catch (err) {
    console.error('Stripe checkout error', err);

    return new Response(JSON.stringify({ error: 'Unable to create checkout session' }), { status: 500 });
  }
};
