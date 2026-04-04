import { vi } from 'vitest';

const mockGenerateSasUrl = vi.fn<() => Promise<string>>().mockResolvedValue(
  'https://example.blob.core.windows.net/fab4kids-products/maths/ks1/pack.pdf?sig=abc',
);

vi.mock('@azure/storage-blob', () => ({
  BlobServiceClient: {
    fromConnectionString: vi.fn(() => ({
      getContainerClient: vi.fn(() => ({
        getBlockBlobClient: vi.fn(() => ({ generateSasUrl: mockGenerateSasUrl })),
      })),
    })),
  },
  BlobSASPermissions: { parse: vi.fn(() => ({})) },
}));

import { generateSignedUrl } from '@/lib/storage.ts';

describe('generateSignedUrl', () => {
  beforeEach(() => {
    vi.resetAllMocks();
    mockGenerateSasUrl.mockResolvedValue(
      'https://example.blob.core.windows.net/fab4kids-products/maths/ks1/pack.pdf?sig=abc',
    );
  });

  it('returns a signed URL for a given blob path', async () => {
    const url = await generateSignedUrl('maths/ks1/pack.pdf');

    expect(url).toContain('blob.core.windows.net');
    expect(url).toContain('sig=');
  });

  it('calls generateSasUrl with read permission and future expiry', async () => {
    const before = Date.now();
    await generateSignedUrl('maths/ks1/pack.pdf');
    const after = Date.now();

    const call = mockGenerateSasUrl.mock.calls[0]?.[0] as { expiresOn: Date };
    expect(call.expiresOn.getTime()).toBeGreaterThan(before);
    expect(call.expiresOn.getTime()).toBeLessThan(after + 20 * 60 * 1000);
  });
});
