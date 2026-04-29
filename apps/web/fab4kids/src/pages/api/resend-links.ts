import type { APIRoute } from 'astro';
import Stripe from 'stripe';
import { generateSignedUrl } from '@/lib/storage.ts';
import { sendDeliveryEmail } from '@/lib/email.ts';
import type { DeliveryLink } from '@/types/index.ts';
import { trackTrace, trackWarning, trackException, trackEvent } from '@/lib/telemetry';

export const prerender = false;

export const POST: APIRoute = async ({ request }) => {
  let body: { orderReference?: unknown; email?: unknown };
  try {
    body = await request.json() as { orderReference?: unknown; email?: unknown };
  } catch {
    trackWarning('resend-links/invalid-body', { reason: 'parse-error' });

    return new Response(JSON.stringify({ error: 'Invalid request body' }), { status: 400 });
  }

  const { orderReference, email } = body;
  if (typeof orderReference !== 'string' || typeof email !== 'string') {
    trackWarning('resend-links/invalid-body', { reason: 'missing-fields' });

    return new Response(JSON.stringify({ error: 'Invalid request' }), { status: 400 });
  }

  const stripe = new Stripe(import.meta.env.STRIPE_SECRET_KEY);

  try {
    trackTrace('resend-links/start', { orderReference });

    const session = await stripe.checkout.sessions.retrieve(orderReference, {
      expand: ['line_items.data.price.product'],
    });

    if (session.customer_details?.email?.toLowerCase() !== email.toLowerCase()) {
      trackWarning('resend-links/email-mismatch', { orderReference });

      return new Response(JSON.stringify({ error: 'Invalid order reference or email' }), { status: 400 });
    }

    const links: DeliveryLink[] = [];
    for (const item of session.line_items?.data ?? []) {
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

    await sendDeliveryEmail(email, orderReference, links);

    trackEvent('resend-links/sent', { orderReference, linkCount: String(links.length) });

    return new Response(JSON.stringify({ success: true }), { status: 200 });
  } catch (err) {
    const error = err instanceof Error ? err : new Error(String(err));
    trackException(error, { context: 'resend-links/failed', orderReference });

    return new Response(JSON.stringify({ error: 'Unable to resend links' }), { status: 500 });
  }
};
