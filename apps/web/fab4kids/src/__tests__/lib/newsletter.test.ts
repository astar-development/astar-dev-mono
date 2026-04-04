import { vi } from 'vitest';

const mockExistsSync = vi.hoisted(() => vi.fn<() => boolean>().mockReturnValue(false));
const mockReadFileSync = vi.hoisted(() => vi.fn<() => string>());
const mockWriteFileSync = vi.hoisted(() => vi.fn<() => void>());
const mockMkdirSync = vi.hoisted(() => vi.fn<() => void>());

vi.mock('fs', () => ({
  default: {
    existsSync: mockExistsSync,
    readFileSync: mockReadFileSync,
    writeFileSync: mockWriteFileSync,
    mkdirSync: mockMkdirSync,
  },
  existsSync: mockExistsSync,
  readFileSync: mockReadFileSync,
  writeFileSync: mockWriteFileSync,
  mkdirSync: mockMkdirSync,
}));

import { addSubscriber } from '@/lib/newsletter.ts';

describe('newsletter', () => {
  beforeEach(() => {
    vi.resetAllMocks();
    mockExistsSync.mockReturnValue(false);
  });

  it('returns an empty array when data file does not exist', () => {
    mockExistsSync.mockReturnValue(false);

    const result = addSubscriber('new@example.com');

    expect(result).toBe('added');
  });

  it('adds a new subscriber and returns added', () => {
    mockExistsSync.mockReturnValue(true);
    mockReadFileSync.mockReturnValue(JSON.stringify([]));

    const result = addSubscriber('test@example.com');

    expect(result).toBe('added');
    expect(mockWriteFileSync).toHaveBeenCalled();
  });

  it('returns duplicate without writing when email already exists', () => {
    mockExistsSync.mockReturnValue(true);
    mockReadFileSync.mockReturnValue(
      JSON.stringify([{ email: 'existing@example.com', subscribedAt: '2026-01-01T00:00:00.000Z' }]),
    );

    const result = addSubscriber('existing@example.com');

    expect(result).toBe('duplicate');
    expect(mockWriteFileSync).not.toHaveBeenCalled();
  });

  it('is case-insensitive when checking for duplicates', () => {
    mockExistsSync.mockReturnValue(true);
    mockReadFileSync.mockReturnValue(
      JSON.stringify([{ email: 'test@example.com', subscribedAt: '2026-01-01T00:00:00.000Z' }]),
    );

    const result = addSubscriber('TEST@EXAMPLE.COM');

    expect(result).toBe('duplicate');
  });
});
