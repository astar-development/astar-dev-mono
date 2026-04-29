import { vi, describe, it, expect, beforeEach, afterEach } from 'vitest';

const mockTrackTrace = vi.fn();
const mockTrackException = vi.fn();
const mockTrackEvent = vi.fn();

const mockClient = {
  trackTrace: mockTrackTrace,
  trackException: mockTrackException,
  trackEvent: mockTrackEvent,
};

const mockSetup = vi.fn().mockReturnValue({
  setAutoCollectRequests: vi.fn().mockReturnThis(),
  setAutoCollectExceptions: vi.fn().mockReturnThis(),
  setAutoCollectDependencies: vi.fn().mockReturnThis(),
  setAutoCollectConsole: vi.fn().mockReturnThis(),
  start: vi.fn().mockReturnThis(),
});

vi.mock('applicationinsights', () => ({
  default: {
    setup: mockSetup,
    get defaultClient() { return mockClient; },
  },
}));

describe('GivenTelemetryWithConnectionString', () => {
  const originalEnv = process.env;

  beforeEach(() => {
    process.env = { ...originalEnv, APPLICATIONINSIGHTS_CONNECTION_STRING: 'InstrumentationKey=test-key' };
    vi.resetModules();
    mockTrackTrace.mockReset();
    mockTrackException.mockReset();
    mockTrackEvent.mockReset();
  });

  afterEach(() => {
    process.env = originalEnv;
  });

  it('when_track_trace_called_then_forwards_to_client', async () => {
    const { trackTrace } = await import('@/lib/telemetry');

    trackTrace('test-message', { key: 'value' });

    expect(mockTrackTrace).toHaveBeenCalledWith(
      expect.objectContaining({ message: 'test-message', properties: { key: 'value' } }),
    );
  });

  it('when_track_warning_called_then_forwards_with_warning_severity', async () => {
    const { trackWarning } = await import('@/lib/telemetry');

    trackWarning('warn-message', { key: 'value' });

    expect(mockTrackTrace).toHaveBeenCalledWith(
      expect.objectContaining({ message: 'warn-message', severity: 2 }),
    );
  });

  it('when_track_exception_called_then_forwards_error_to_client', async () => {
    const { trackException } = await import('@/lib/telemetry');
    const error = new Error('boom');

    trackException(error, { context: 'test' });

    expect(mockTrackException).toHaveBeenCalledWith(
      expect.objectContaining({ exception: error, properties: { context: 'test' } }),
    );
  });

  it('when_track_event_called_then_forwards_to_client', async () => {
    const { trackEvent } = await import('@/lib/telemetry');

    trackEvent('my-event', { foo: 'bar' });

    expect(mockTrackEvent).toHaveBeenCalledWith(
      expect.objectContaining({ name: 'my-event', properties: { foo: 'bar' } }),
    );
  });
});

describe('GivenTelemetryWithoutConnectionString', () => {
  const originalEnv = process.env;

  beforeEach(() => {
    process.env = { ...originalEnv };
    delete process.env['APPLICATIONINSIGHTS_CONNECTION_STRING'];
    vi.resetModules();
    mockTrackTrace.mockReset();
    mockTrackException.mockReset();
    mockTrackEvent.mockReset();
  });

  afterEach(() => {
    process.env = originalEnv;
  });

  it('when_track_trace_called_then_no_op', async () => {
    vi.doMock('applicationinsights', () => ({
      default: {
        setup: mockSetup,
        defaultClient: null,
      },
    }));

    const { trackTrace } = await import('@/lib/telemetry');

    expect(() => trackTrace('message')).not.toThrow();
    expect(mockTrackTrace).not.toHaveBeenCalled();
  });

  it('when_track_exception_called_then_no_op', async () => {
    vi.doMock('applicationinsights', () => ({
      default: {
        setup: mockSetup,
        defaultClient: null,
      },
    }));

    const { trackException } = await import('@/lib/telemetry');

    expect(() => trackException(new Error('boom'))).not.toThrow();
    expect(mockTrackException).not.toHaveBeenCalled();
  });
});
