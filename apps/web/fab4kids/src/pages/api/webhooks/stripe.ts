import type { APIRoute } from 'astro';
import Stripe from 'stripe';
import { isAlreadyProcessed, markAsProcessed } from '@/lib/idempotency.ts';
import { generateSignedUrl } from '@/lib/storage.ts';
import { sendDeliveryEmail } from '@/lib/email.ts';
import type { DeliveryLink } from '@/types/index.ts';
import { trackTrace, trackWarning, trackException, trackEvent } from '@/lib/telemetry';

export const prerender = false;

export const POST: APIRoute = async ({ request }) => {
  const stripe = new Stripe(import.meta.env.STRIPE_SECRET_KEY);
  const sig = request.headers.get('stripe-signature');
  const body = await request.text();

  if (!sig) {
    trackWarning('webhook/stripe/missing-signature');

    return new Response('Missing stripe-signature header', { status: 400 });
  }

  let event: Stripe.Event;
  try {
    event = stripe.webhooks.constructEvent(body, sig, import.meta.env.STRIPE_WEBHOOK_SECRET);
  } catch (err) {
    const error = err instanceof Error ? err : new Error(String(err));
    trackException(error, { context: 'webhook/stripe/signature-verification-failed' });

    return new Response('Invalid signature', { status: 400 });
  }

  if (event.type !== 'checkout.session.completed') {
    return new Response('OK', { status: 200 });
  }

  const session = event.data.object as Stripe.Checkout.Session;
  const sessionId = session.id;

  if (isAlreadyProcessed(sessionId)) {
    trackTrace('webhook/stripe/duplicate-event', { sessionId });

    return new Response('OK', { status: 200 });
  }

  markAsProcessed(sessionId);

  const customerEmail = session.customer_details?.email;
  if (!customerEmail) {
    trackWarning('webhook/stripe/no-customer-email', { sessionId });

    return new Response('OK', { status: 200 });
  }

  try {
    trackTrace('webhook/stripe/delivery-start', { sessionId });

    const fullSession = await stripe.checkout.sessions.retrieve(sessionId, {
      expand: ['line_items.data.price.product'],
    });

    const links: DeliveryLink[] = [];
    for (const item of fullSession.line_items?.data ?? []) {
      const product = item.price?.product as Stripe.Product | undefined;
      const blobPath = product?.metadata?.blobPath;
      if (!blobPath) continue;

      const url = await generateSignedUrl(blobPath);
      links.push({
        productTitle: product?.name ?? 'Resource',
        url,
        expiresAt: new Date(Date.now() + 15 * 60 * 1000).toISOString(),
      });
    }

    await sendDeliveryEmail(customerEmail, sessionId, links);

    trackEvent('webhook/stripe/delivery-sent', { sessionId, linkCount: String(links.length) });
  } catch (err) {
    const error = err instanceof Error ? err : new Error(String(err));
    trackException(error, { context: 'webhook/stripe/delivery-failed', sessionId });
  }

  return new Response('OK', { status: 200 });
};
