import type { APIRoute } from 'astro';
import Stripe from 'stripe';
import { generateSignedUrl } from '@/lib/storage.ts';
import { sendDeliveryEmail } from '@/lib/email.ts';
import type { DeliveryLink } from '@/types/index.ts';

export const prerender = false;

export const POST: APIRoute = async ({ request }) => {
  let body: { orderReference?: unknown; email?: unknown };
  try {
    body = await request.json() as { orderReference?: unknown; email?: unknown };
  } catch {
    return new Response(JSON.stringify({ error: 'Invalid request body' }), { status: 400 });
  }

  const { orderReference, email } = body;
  if (typeof orderReference !== 'string' || typeof email !== 'string') {
    return new Response(JSON.stringify({ error: 'Invalid request' }), { status: 400 });
  }

  const stripe = new Stripe(import.meta.env.STRIPE_SECRET_KEY);

  try {
    const session = await stripe.checkout.sessions.retrieve(orderReference, {
      expand: ['line_items.data.price.product'],
    });

    if (session.customer_details?.email?.toLowerCase() !== email.toLowerCase()) {
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

    return new Response(JSON.stringify({ success: true }), { status: 200 });
  } catch (err) {
    console.error('Resend links error', err);

    return new Response(JSON.stringify({ error: 'Unable to resend links' }), { status: 500 });
  }
};
