import { Resend } from 'resend';
import type { DeliveryLink } from '@/types/index.ts';

function getClient(): Resend {
  return new Resend(import.meta.env.RESEND_API_KEY);
}

function buildDeliveryHtml(orderReference: string, links: DeliveryLink[]): string {
  const items = links
    .map(
      (l) =>
        `<li><strong>${l.productTitle}</strong><br>
         <a href="${l.url}">Download</a> (expires ${new Date(l.expiresAt).toLocaleTimeString('en-GB')})</li>`,
    )
    .join('');

  return `
    <h1>Your fab4kids order is ready!</h1>
    <p>Order reference: <code>${orderReference}</code></p>
    <p>Your download links are below. Each link expires in ${import.meta.env.SIGNED_URL_TTL_MINUTES ?? 15} minutes.</p>
    <ul>${items}</ul>
    <p>If your links expire, visit your order confirmation page to request new ones.</p>
    <p>Thank you for supporting fab4kids!</p>
  `;
}

function buildDeliveryText(orderReference: string, links: DeliveryLink[]): string {
  const items = links
    .map((l) => `${l.productTitle}: ${l.url} (expires ${l.expiresAt})`)
    .join('\n');

  return `Your fab4kids order is ready!\n\nOrder: ${orderReference}\n\n${items}\n\nThank you!`;
}

export async function sendDeliveryEmail(to: string, orderReference: string, links: DeliveryLink[]): Promise<void> {
  const resend = getClient();
  const { error } = await resend.emails.send({
    from: import.meta.env.RESEND_FROM_ADDRESS,
    to,
    subject: 'Your fab4kids order is ready to download',
    html: buildDeliveryHtml(orderReference, links),
    text: buildDeliveryText(orderReference, links),
  });

  if (error) {
    throw new Error(`Resend error: ${error.message}`);
  }
}

export async function sendNewsletterConfirmation(to: string): Promise<void> {
  const resend = getClient();
  const { error } = await resend.emails.send({
    from: import.meta.env.RESEND_FROM_ADDRESS,
    to,
    subject: 'Welcome to fab4kids updates!',
    html: '<h1>Thanks for signing up!</h1><p>We\'ll keep you updated with the latest educational resources.</p>',
    text: "Thanks for signing up! We'll keep you updated with the latest educational resources.",
  });

  if (error) {
    throw new Error(`Resend error: ${error.message}`);
  }
}
