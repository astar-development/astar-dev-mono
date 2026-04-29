import { vi } from 'vitest';
import type Stripe from 'stripe';

const mockSessionCreate = vi.hoisted(() => vi.fn<() => Promise<Partial<Stripe.Checkout.Session>>>());
const mockSessionRetrieve = vi.hoisted(() => vi.fn<() => Promise<Partial<Stripe.Checkout.Session>>>());
const mockConstructEvent = vi.hoisted(() => vi.fn<() => Stripe.Event>());
const mockSendDeliveryEmail = vi.hoisted(() => vi.fn<() => Promise<void>>().mockResolvedValue(undefined));
const mockGenerateSignedUrl = vi.hoisted(() => vi.fn<() => Promise<string>>().mockResolvedValue('https://example.com/signed'));
const mockIsAlreadyProcessed = vi.hoisted(() => vi.fn<() => boolean>().mockReturnValue(false));
const mockMarkAsProcessed = vi.hoisted(() => vi.fn<() => void>());

vi.mock('stripe', () => ({
  default: class MockStripe {
    checkout = { sessions: { create: mockSessionCreate, retrieve: mockSessionRetrieve } };
    webhooks = { constructEvent: mockConstructEvent };
  },
}));
vi.mock('@/lib/email.ts', () => ({ sendDeliveryEmail: mockSendDeliveryEmail }));
vi.mock('@/lib/storage.ts', () => ({ generateSignedUrl: mockGenerateSignedUrl }));
vi.mock('@/lib/idempotency.ts', () => ({ isAlreadyProcessed: mockIsAlreadyProcessed, markAsProcessed: mockMarkAsProcessed }));

import { POST as checkoutPost } from '@/pages/api/checkout.ts';
import { POST as webhookPost } from '@/pages/api/webhooks/stripe.ts';

function makeRequest(body: unknown, headers: Record<string, string> = {}): Request {
  return new Request('http://localhost/api/checkout', {
    method: 'POST',
    body: typeof body === 'string' ? body : JSON.stringify(body),
    headers: { 'Content-Type': 'application/json', ...headers },
  });
}

describe('POST /api/checkout', () => {
  beforeEach(() => {
    vi.resetAllMocks();
    mockSessionCreate.mockResolvedValue({ url: 'https://checkout.stripe.com/test-session' });
  });

  it('creates a Stripe session with the correct line items and returns the URL', async () => {
    const request = makeRequest({
      items: [
        { stripePriceId: 'price_abc', quantity: 1 },
        { stripePriceId: 'price_xyz', quantity: 2 },
      ],
    });

    const response = await checkoutPost({ request } as Parameters<typeof checkoutPost>[0]);
    const body = await response.json() as { url: string };

    expect(response.status).toBe(200);
    expect(body.url).toBe('https://checkout.stripe.com/test-session');
    expect(mockSessionCreate).toHaveBeenCalledWith(
      expect.objectContaining({
        mode: 'payment',
        line_items: [
          { price: 'price_abc', quantity: 1 },
          { price: 'price_xyz', quantity: 2 },
        ],
      }),
    );
  });

  it('returns 400 when cart is empty', async () => {
    const request = makeRequest({ items: [] });

    const response = await checkoutPost({ request } as Parameters<typeof checkoutPost>[0]);

    expect(response.status).toBe(400);
  });

  it('returns 400 when request body is not valid JSON', async () => {
    const request = new Request('http://localhost/api/checkout', {
      method: 'POST',
      body: 'not-json',
      headers: { 'Content-Type': 'application/json' },
    });

    const response = await checkoutPost({ request } as Parameters<typeof checkoutPost>[0]);

    expect(response.status).toBe(400);
  });

  it('returns 500 when Stripe throws', async () => {
    mockSessionCreate.mockRejectedValueOnce(new Error('Stripe error'));
    const request = makeRequest({ items: [{ stripePriceId: 'price_abc', quantity: 1 }] });

    const response = await checkoutPost({ request } as Parameters<typeof checkoutPost>[0]);

    expect(response.status).toBe(500);
  });
});

describe('POST /api/webhooks/stripe', () => {
  const makeWebhookRequest = (body: string, sig: string) =>
    new Request('http://localhost/api/webhooks/stripe', {
      method: 'POST',
      body,
      headers: { 'stripe-signature': sig, 'Content-Type': 'application/json' },
    });

  const validSessionEvent = (sessionId: string, email: string | null): Stripe.Event => ({
    type: 'checkout.session.completed',
    data: {
      object: {
        id: sessionId,
        customer_details: email ? { email } : null,
        object: 'checkout.session',
      } as Stripe.Checkout.Session,
    },
  } as Stripe.Event);

  beforeEach(() => {
    vi.resetAllMocks();
    mockIsAlreadyProcessed.mockReturnValue(false);
    mockGenerateSignedUrl.mockResolvedValue('https://blob.example.com/signed-url');
    mockSendDeliveryEmail.mockResolvedValue(undefined);
    mockSessionRetrieve.mockResolvedValue({
      id: 'sess_test',
      line_items: { data: [] },
    });
  });

  it('returns 400 when stripe-signature header is missing', async () => {
    const request = new Request('http://localhost/api/webhooks/stripe', {
      method: 'POST',
      body: '{}',
    });

    const response = await webhookPost({ request } as Parameters<typeof webhookPost>[0]);

    expect(response.status).toBe(400);
  });

  it('returns 400 when Stripe signature verification fails', async () => {
    mockConstructEvent.mockImplementation(() => {
      throw new Error('Invalid signature');
    });
    const request = makeWebhookRequest('{}', 'bad-sig');

    const response = await webhookPost({ request } as Parameters<typeof webhookPost>[0]);

    expect(response.status).toBe(400);
  });

  it('returns 200 without processing when session is already processed (idempotency)', async () => {
    mockIsAlreadyProcessed.mockReturnValue(true);
    mockConstructEvent.mockReturnValue(validSessionEvent('sess_duplicate', 'user@example.com'));
    const request = makeWebhookRequest('{}', 'valid-sig');

    const response = await webhookPost({ request } as Parameters<typeof webhookPost>[0]);

    expect(response.status).toBe(200);
    expect(mockSendDeliveryEmail).not.toHaveBeenCalled();
  });

  it('returns 200 without sending email when customer email is missing', async () => {
    mockConstructEvent.mockReturnValue(validSessionEvent('sess_no_email', null));
    const request = makeWebhookRequest('{}', 'valid-sig');

    const response = await webhookPost({ request } as Parameters<typeof webhookPost>[0]);

    expect(response.status).toBe(200);
    expect(mockSendDeliveryEmail).not.toHaveBeenCalled();
  });

  it('sends delivery email to customer email address on successful checkout', async () => {
    mockConstructEvent.mockReturnValue(validSessionEvent('sess_ok', 'buyer@example.com'));
    mockSessionRetrieve.mockResolvedValue({
      id: 'sess_ok',
      line_items: {
        data: [{
          price: {
            product: {
              name: 'Maths Pack',
              metadata: { blobPath: 'maths/ks1/pack.pdf' },
            },
          },
        }],
      },
    });
    const request = makeWebhookRequest('{}', 'valid-sig');

    await webhookPost({ request } as Parameters<typeof webhookPost>[0]);

    expect(mockSendDeliveryEmail).toHaveBeenCalledWith(
      'buyer@example.com',
      'sess_ok',
      expect.arrayContaining([expect.objectContaining({ url: 'https://blob.example.com/signed-url' })]),
    );
  });
});
