import { vi } from 'vitest';
import type { DeliveryLink } from '@/types/index.ts';

const mockSend = vi.hoisted(() =>
  vi.fn<() => Promise<{ data: { id: string } | null; error: { message: string } | null }>>()
    .mockResolvedValue({ data: { id: '123' }, error: null }),
);

vi.mock('resend', () => ({
  Resend: class MockResend {
    emails = { send: mockSend };
  },
}));

import { sendDeliveryEmail } from '@/lib/email.ts';

const LINKS: DeliveryLink[] = [
  { productTitle: 'Year 3 Maths Pack', url: 'https://example.com/dl/1', expiresAt: '2026-04-04T10:00:00Z' },
  { productTitle: 'Year 4 English Pack', url: 'https://example.com/dl/2', expiresAt: '2026-04-04T10:00:00Z' },
];

describe('sendDeliveryEmail', () => {
  beforeEach(() => {
    mockSend.mockReset();
    mockSend.mockResolvedValue({ data: { id: '123' }, error: null });
  });

  it('sends delivery email to the correct address', async () => {
    await sendDeliveryEmail('customer@example.com', 'sess_abc', LINKS);

    expect(mockSend).toHaveBeenCalledWith(
      expect.objectContaining({ to: 'customer@example.com' }),
    );
  });

  it('includes all download links in the email', async () => {
    await sendDeliveryEmail('customer@example.com', 'sess_abc', LINKS);

    const call = mockSend.mock.calls[0]?.[0] as { html: string };
    expect(call.html).toContain('Year 3 Maths Pack');
    expect(call.html).toContain('Year 4 English Pack');
  });

  it('throws when Resend returns an error', async () => {
    mockSend.mockResolvedValueOnce({ data: null, error: { message: 'API error' } });

    await expect(sendDeliveryEmail('x@y.com', 'sess_err', LINKS)).rejects.toThrow('API error');
  });
});
