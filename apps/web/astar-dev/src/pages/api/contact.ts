import type { APIRoute } from 'astro';
import sgMail from '@sendgrid/mail';

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
const RATE_LIMIT_WINDOW_MS = 15 * 60 * 1000; // 15 minutes
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
// SendGrid helpers
// ---------------------------------------------------------------------------

function buildOwnerEmail(name: string, email: string, message: string, to: string, from: string): sgMail.MailDataRequired {
  const timestamp = new Date().toISOString();
  return {
    to,
    from,
    subject: `[AStar.Dev Contact] Message from ${name}`,
    text: [
      `Name: ${name}`,
      `Email: ${email}`,
      `Timestamp: ${timestamp}`,
      '',
      'Message:',
      message,
    ].join('\n'),
    html: [
      `<p><strong>Name:</strong> ${escapeHtml(name)}</p>`,
      `<p><strong>Email:</strong> ${escapeHtml(email)}</p>`,
      `<p><strong>Timestamp:</strong> ${timestamp}</p>`,
      `<hr />`,
      `<p><strong>Message:</strong></p>`,
      `<p>${escapeHtml(message).replace(/\n/g, '<br />')}</p>`,
    ].join('\n'),
  };
}

function buildCopyEmail(name: string, email: string, message: string, from: string): sgMail.MailDataRequired {
  return {
    to: email,
    from,
    subject: 'Copy of your message to AStar Development',
    text: [
      `Hi ${name},`,
      '',
      'This is a copy of the message you sent to AStar Development.',
      '',
      'We will be in touch as soon as possible.',
      '',
      '---',
      '',
      message,
    ].join('\n'),
    html: [
      `<p>Hi ${escapeHtml(name)},</p>`,
      `<p>This is a copy of the message you sent to AStar Development.</p>`,
      `<p>We will be in touch as soon as possible.</p>`,
      `<hr />`,
      `<p>${escapeHtml(message).replace(/\n/g, '<br />')}</p>`,
    ].join('\n'),
  };
}

function escapeHtml(text: string): string {
  return text
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#039;');
}

// ---------------------------------------------------------------------------
// Route handler
// ---------------------------------------------------------------------------

export const POST: APIRoute = async ({ request }) => {
  // Body size check — reject payloads >= 10KB
  const contentLength = request.headers.get('content-length');
  if (contentLength !== null && parseInt(contentLength, 10) >= 10_240) {
    return new Response(JSON.stringify({ errors: [{ field: '', message: 'Request body too large.' }] }), {
      status: 400,
      headers: { 'Content-Type': 'application/json' },
    });
  }

  // IP-based rate limiting
  const ip =
    request.headers.get('x-forwarded-for')?.split(',')[0]?.trim() ??
    request.headers.get('x-real-ip') ??
    'unknown';

  if (!checkRateLimit(ip)) {
    console.warn(`[contact] Rate limit exceeded for IP: ${ip}`);
    return new Response(JSON.stringify({ message: 'Too many requests. Please try again in 15 minutes.' }), {
      status: 429,
      headers: { 'Content-Type': 'application/json' },
    });
  }

  // Parse body
  let body: ContactBody;
  try {
    const raw: unknown = await request.json();
    if (typeof raw !== 'object' || raw === null) {
      return new Response(JSON.stringify({ errors: [{ field: '', message: 'Invalid request body.' }] }), {
        status: 400,
        headers: { 'Content-Type': 'application/json' },
      });
    }
    body = raw as ContactBody;
  } catch {
    return new Response(JSON.stringify({ errors: [{ field: '', message: 'Invalid request body.' }] }), {
      status: 400,
      headers: { 'Content-Type': 'application/json' },
    });
  }

  // Honeypot check — silent success, no email sent
  const website = typeof body.website === 'string' ? body.website : '';
  if (website.length > 0) {
    console.warn(`[contact] Honeypot triggered from IP: ${ip}`);
    return new Response(JSON.stringify({ message: 'Thank you for your message.' }), {
      status: 200,
      headers: { 'Content-Type': 'application/json' },
    });
  }

  // Server-side validation
  const errors = validateBody(body);
  if (errors.length > 0) {
    return new Response(JSON.stringify({ errors }), {
      status: 400,
      headers: { 'Content-Type': 'application/json' },
    });
  }

  const name = (body.name as string).trim();
  const email = (body.email as string).trim();
  const message = (body.message as string).trim();
  const sendCopy = body.sendCopy === true;

  // Read environment variables
  const apiKey = import.meta.env.SENDGRID_API_KEY;
  const contactEmail = import.meta.env.CONTACT_EMAIL;
  const fromEmail = import.meta.env.SENDGRID_FROM_EMAIL;

  if (typeof apiKey !== 'string' || apiKey.length === 0) {
    console.error('[contact] SENDGRID_API_KEY is not configured');
    return new Response(JSON.stringify({ message: `Something went wrong. Please email ${contactEmail ?? 'us'} directly.` }), {
      status: 500,
      headers: { 'Content-Type': 'application/json' },
    });
  }

  if (typeof contactEmail !== 'string' || contactEmail.length === 0 || typeof fromEmail !== 'string' || fromEmail.length === 0) {
    console.error('[contact] CONTACT_EMAIL or SENDGRID_FROM_EMAIL is not configured');
    return new Response(JSON.stringify({ message: 'Something went wrong. Please try again later.' }), {
      status: 500,
      headers: { 'Content-Type': 'application/json' },
    });
  }

  // Send via SendGrid
  try {
    sgMail.setApiKey(apiKey);

    const messages: sgMail.MailDataRequired[] = [
      buildOwnerEmail(name, email, message, contactEmail, fromEmail),
    ];

    if (sendCopy) {
      messages.push(buildCopyEmail(name, email, message, fromEmail));
    }

    await sgMail.send(messages.length === 1 ? messages[0] : messages);

    return new Response(JSON.stringify({ message: 'Thank you for your message. We will be in touch soon.' }), {
      status: 200,
      headers: { 'Content-Type': 'application/json' },
    });
  } catch (err: unknown) {
    console.error('[contact] SendGrid error:', err);
    return new Response(JSON.stringify({ message: `Something went wrong. Please email ${contactEmail} directly.` }), {
      status: 500,
      headers: { 'Content-Type': 'application/json' },
    });
  }
};
