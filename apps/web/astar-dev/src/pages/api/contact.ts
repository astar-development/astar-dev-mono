import type { APIRoute } from 'astro';
import Mailjet from 'node-mailjet';
import { trackTrace, trackWarning, trackException, trackEvent } from '../../lib/telemetry';

export const prerender = false;

// ---------------------------------------------------------------------------
// Types
// ---------------------------------------------------------------------------

interface RateLimitEntry {
  count: number;
  resetAt: number;
}

interface ContactBody {
  name: unknown;
  email: unknown;
  message: unknown;
  sendCopy: unknown;
  website: unknown;
}

type ValidationError = { field: string; message: string };

// ---------------------------------------------------------------------------
// Rate limiter — in-memory Map, sufficient for single-instance deployment
// ---------------------------------------------------------------------------

const RATE_LIMIT_MAX = 10;
const RATE_LIMIT_WINDOW_MS = 15 * 60 * 1000;
const rateLimitMap = new Map<string, RateLimitEntry>();

function checkRateLimit(ip: string): boolean {
  const now = Date.now();
  const entry = rateLimitMap.get(ip);

  if (entry === undefined || now > entry.resetAt) {
    rateLimitMap.set(ip, { count: 1, resetAt: now + RATE_LIMIT_WINDOW_MS });

    return true;
  }

  if (entry.count >= RATE_LIMIT_MAX) {

    return false;
  }

  entry.count += 1;

  return true;
}

// ---------------------------------------------------------------------------
// Validation helpers
// ---------------------------------------------------------------------------

const EMAIL_RE = /^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)+$/;

function validateBody(body: ContactBody): ValidationError[] {
  const errors: ValidationError[] = [];

  const name = typeof body.name === 'string' ? body.name.trim() : '';
  const email = typeof body.email === 'string' ? body.email.trim() : '';
  const message = typeof body.message === 'string' ? body.message.trim() : '';

  if (name.length === 0) {
    errors.push({ field: 'name', message: 'Name is required.' });
  } else if (name.length > 200) {
    errors.push({ field: 'name', message: 'Name must be 200 characters or fewer.' });
  }

  if (email.length === 0) {
    errors.push({ field: 'email', message: 'Email is required.' });
  } else if (!EMAIL_RE.test(email)) {
    errors.push({ field: 'email', message: 'Please enter a valid email address.' });
  }

  if (message.length === 0) {
    errors.push({ field: 'message', message: 'Message is required.' });
  } else if (message.length < 10) {
    errors.push({ field: 'message', message: 'Message must be at least 10 characters.' });
  } else if (message.length > 5000) {
    errors.push({ field: 'message', message: 'Message must be 5000 characters or fewer.' });
  }

  return errors;
}

// ---------------------------------------------------------------------------
// Mailjet helpers
// ---------------------------------------------------------------------------

function escapeHtml(text: string): string {
  return text
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#039;');
}

interface MailjetMessage {
  From: { Email: string; Name: string };
  To: [{ Email: string; Name: string }];
  Subject: string;
  TextPart: string;
  HTMLPart: string;
}

function buildOwnerMessage(name: string, email: string, message: string, to: string, from: string): MailjetMessage {
  const timestamp = new Date().toISOString();

  return {
    From: { Email: from, Name: 'AStar Development' },
    To: [{ Email: to, Name: 'AStar Development' }],
    Subject: `[AStar.Dev Contact] Message from ${name}`,
    TextPart: [`Name: ${name}`, `Email: ${email}`, `Timestamp: ${timestamp}`, '', 'Message:', message].join('\n'),
    HTMLPart: [
      `<p><strong>Name:</strong> ${escapeHtml(name)}</p>`,
      `<p><strong>Email:</strong> ${escapeHtml(email)}</p>`,
      `<p><strong>Timestamp:</strong> ${timestamp}</p>`,
      `<hr />`,
      `<p><strong>Message:</strong></p>`,
      `<p>${escapeHtml(message).replace(/\n/g, '<br />')}</p>`,
    ].join('\n'),
  };
}

function buildCopyMessage(name: string, email: string, message: string, from: string): MailjetMessage {
  return {
    From: { Email: from, Name: 'AStar Development' },
    To: [{ Email: email, Name: name }],
    Subject: 'Copy of your message to AStar Development',
    TextPart: [`Hi ${name},`, '', 'This is a copy of the message you sent to AStar Development.', '', 'We will be in touch as soon as possible.', '', '---', '', message].join('\n'),
    HTMLPart: [
      `<p>Hi ${escapeHtml(name)},</p>`,
      `<p>This is a copy of the message you sent to AStar Development.</p>`,
      `<p>We will be in touch as soon as possible.</p>`,
      `<hr />`,
      `<p>${escapeHtml(message).replace(/\n/g, '<br />')}</p>`,
    ].join('\n'),
  };
}

// ---------------------------------------------------------------------------
// Route handler
// ---------------------------------------------------------------------------

export const POST: APIRoute = async ({ request }) => {
  const contentLength = request.headers.get('content-length');
  if (contentLength !== null && parseInt(contentLength, 10) >= 10_240) {
    trackWarning('contact/request-too-large', { contentLength });

    return new Response(JSON.stringify({ errors: [{ field: '', message: 'Request body too large.' }] }), {
      status: 400,
      headers: { 'Content-Type': 'application/json' },
    });
  }

  const ip =
    request.headers.get('x-forwarded-for')?.split(',')[0]?.trim() ??
    request.headers.get('x-real-ip') ??
    'unknown';

  if (!checkRateLimit(ip)) {
    trackWarning('contact/rate-limit-exceeded', { ip });

    return new Response(JSON.stringify({ message: 'Too many requests. Please try again in 15 minutes.' }), {
      status: 429,
      headers: { 'Content-Type': 'application/json', 'Retry-After': '900' },
    });
  }

  let body: ContactBody;
  try {
    const raw: unknown = await request.json();
    if (typeof raw !== 'object' || raw === null) {
      trackWarning('contact/invalid-body', { ip, reason: 'not-an-object' });

      return new Response(JSON.stringify({ errors: [{ field: '', message: 'Invalid request body.' }] }), {
        status: 400,
        headers: { 'Content-Type': 'application/json' },
      });
    }
    body = raw as ContactBody;
  } catch {
    trackWarning('contact/invalid-body', { ip, reason: 'parse-error' });

    return new Response(JSON.stringify({ errors: [{ field: '', message: 'Invalid request body.' }] }), {
      status: 400,
      headers: { 'Content-Type': 'application/json' },
    });
  }

  const website = typeof body.website === 'string' ? body.website : '';
  if (website.length > 0) {
    trackWarning('contact/honeypot-triggered', { ip });

    return new Response(JSON.stringify({ message: 'Thank you for your message.' }), {
      status: 200,
      headers: { 'Content-Type': 'application/json' },
    });
  }

  const errors = validateBody(body);
  if (errors.length > 0) {
    trackTrace('contact/validation-failed', { ip, fields: errors.map((e) => e.field).join(',') });

    return new Response(JSON.stringify({ errors }), {
      status: 400,
      headers: { 'Content-Type': 'application/json' },
    });
  }

  const name = (body.name as string).trim();
  const email = (body.email as string).trim();
  const message = (body.message as string).trim();
  const sendCopy = body.sendCopy === true;

  const apiKey = import.meta.env.MJ_APIKEY_PUBLIC;
  const apiSecret = import.meta.env.MJ_APIKEY_PRIVATE;
  const contactEmail = import.meta.env.CONTACT_EMAIL;
  const fromEmail = import.meta.env.MAILJET_FROM_EMAIL;

  if (typeof apiKey !== 'string' || apiKey.length === 0 || typeof apiSecret !== 'string' || apiSecret.length === 0) {
    trackException(new Error('contact/missing-mailjet-credentials: MJ_APIKEY_PUBLIC or MJ_APIKEY_PRIVATE is not configured'));

    return new Response(JSON.stringify({ message: `Something went wrong. Please email ${contactEmail ?? 'us'} directly.` }), {
      status: 500,
      headers: { 'Content-Type': 'application/json' },
    });
  }

  if (typeof contactEmail !== 'string' || contactEmail.length === 0 || typeof fromEmail !== 'string' || fromEmail.length === 0) {
    trackException(new Error('contact/missing-email-config: CONTACT_EMAIL or MAILJET_FROM_EMAIL is not configured'));

    return new Response(JSON.stringify({ message: 'Something went wrong. Please try again later.' }), {
      status: 500,
      headers: { 'Content-Type': 'application/json' },
    });
  }

  try {
    trackTrace('contact/send-start', { ip, sendCopy: String(sendCopy), messageLength: String(message.length) });

    const client = Mailjet.apiConnect(apiKey, apiSecret);

    const messages: MailjetMessage[] = [buildOwnerMessage(name, email, message, contactEmail, fromEmail)];
    if (sendCopy) {
      messages.push(buildCopyMessage(name, email, message, fromEmail));
    }

    await client.post('send', { version: 'v3.1' }).request({ Messages: messages });

    trackEvent('contact/send-success', { ip, sendCopy: String(sendCopy), messageCount: String(messages.length) });

    return new Response(JSON.stringify({ message: 'Thank you for your message. We will be in touch soon.' }), {
      status: 200,
      headers: { 'Content-Type': 'application/json' },
    });
  } catch (err: unknown) {
    const error = err instanceof Error ? err : new Error(String(err));
    trackException(error, { ip, sendCopy: String(sendCopy), context: 'contact/send-failed' });

    return new Response(JSON.stringify({ message: `Something went wrong. Please email ${contactEmail} directly.` }), {
      status: 500,
      headers: { 'Content-Type': 'application/json' },
    });
  }
};
