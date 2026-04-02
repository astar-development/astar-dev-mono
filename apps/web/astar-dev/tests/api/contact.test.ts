import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'

let ipCounter = 0
function uniqueIp(): string {
  ipCounter += 1
  return `10.${Math.floor(ipCounter / 65536) % 256}.${Math.floor(ipCounter / 256) % 256}.${ipCounter % 256}`
}

const mockRequest = vi.fn<() => Promise<unknown>>()

vi.mock('../../src/lib/telemetry', () => ({
  trackTrace: vi.fn(),
  trackWarning: vi.fn(),
  trackException: vi.fn(),
  trackEvent: vi.fn(),
}))

vi.mock('node-mailjet', () => ({
  default: {
    apiConnect: vi.fn<() => { post: (resource: string, options: unknown) => { request: typeof mockRequest } }>(() => ({
      post: vi.fn(() => ({ request: mockRequest })),
    })),
  },
}))

import { POST } from '../../src/pages/api/contact'

type EnvOverrides = {
  MJ_APIKEY_PUBLIC?: string
  MJ_APIKEY_PRIVATE?: string
  CONTACT_EMAIL?: string
  MAILJET_FROM_EMAIL?: string
}

function setEnv(overrides: EnvOverrides = {}): void {
  const defaults: Required<EnvOverrides> = {
    MJ_APIKEY_PUBLIC: 'test-public-key',
    MJ_APIKEY_PRIVATE: 'test-private-key',
    CONTACT_EMAIL: 'owner@example.com',
    MAILJET_FROM_EMAIL: 'noreply@example.com',
  }
  const merged = { ...defaults, ...overrides }
  vi.stubEnv('MJ_APIKEY_PUBLIC', merged.MJ_APIKEY_PUBLIC)
  vi.stubEnv('MJ_APIKEY_PRIVATE', merged.MJ_APIKEY_PRIVATE)
  vi.stubEnv('CONTACT_EMAIL', merged.CONTACT_EMAIL)
  vi.stubEnv('MAILJET_FROM_EMAIL', merged.MAILJET_FROM_EMAIL)
}

function makeRequest(body: unknown, headers: Record<string, string> = {}): Request {
  return new Request('http://localhost/api/contact', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', 'x-forwarded-for': uniqueIp(), ...headers },
    body: JSON.stringify(body),
  })
}

function makeContext(request: Request): Parameters<typeof POST>[0] {
  return { request } as Parameters<typeof POST>[0]
}

const VALID_BODY = { name: 'Alice', email: 'alice@example.com', message: 'Hello there enough chars', website: '', sendCopy: false }

describe('POST /api/contact', () => {
  beforeEach(() => {
    vi.resetAllMocks()
    mockRequest.mockResolvedValue({ body: { Messages: [] } })
    setEnv()
  })

  afterEach(() => {
    vi.unstubAllEnvs()
  })

  it('returns 400 when the content-length header is at the 10 KB limit', async () => {
    const response = await POST(makeContext(makeRequest(VALID_BODY, { 'content-length': '10240' })))

    expect(response.status).toBe(400)
  })

  it('returns a body indicating the payload is too large when content-length is at the limit', async () => {
    const response = await POST(makeContext(makeRequest(VALID_BODY, { 'content-length': '10240' })))
    const body = await response.json() as { errors: Array<{ field: string; message: string }> }

    expect(body.errors[0].message).toBe('Request body too large.')
  })

  it('does not reject a request when content-length is one byte under the limit', async () => {
    const response = await POST(makeContext(makeRequest(VALID_BODY, { 'content-length': '10239' })))

    expect(response.status).not.toBe(400)
  })

  it('returns 429 when the same IP has exceeded the rate limit', async () => {
    const ip = uniqueIp()
    for (let i = 0; i < 10; i++) {
      await POST(makeContext(makeRequest(VALID_BODY, { 'x-forwarded-for': ip })))
    }

    const response = await POST(makeContext(makeRequest(VALID_BODY, { 'x-forwarded-for': ip })))

    expect(response.status).toBe(429)
  })

  it('includes a Retry-After header of 900 seconds on a 429 response', async () => {
    const ip = uniqueIp()
    for (let i = 0; i < 10; i++) {
      await POST(makeContext(makeRequest(VALID_BODY, { 'x-forwarded-for': ip })))
    }

    const response = await POST(makeContext(makeRequest(VALID_BODY, { 'x-forwarded-for': ip })))

    expect(response.headers.get('Retry-After')).toBe('900')
  })

  it('returns a body with a retry message on a 429 response', async () => {
    const ip = uniqueIp()
    for (let i = 0; i < 10; i++) {
      await POST(makeContext(makeRequest(VALID_BODY, { 'x-forwarded-for': ip })))
    }

    const response = await POST(makeContext(makeRequest(VALID_BODY, { 'x-forwarded-for': ip })))
    const body = await response.json() as { message: string }

    expect(body.message).toBe('Too many requests. Please try again in 15 minutes.')
  })

  it('returns 400 when the request body is not valid JSON', async () => {
    const request = new Request('http://localhost/api/contact', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', 'x-forwarded-for': uniqueIp() },
      body: 'not-json{{',
    })

    const response = await POST(makeContext(request))

    expect(response.status).toBe(400)
  })

  it('returns 200 silently when the honeypot website field is non-empty', async () => {
    const response = await POST(makeContext(makeRequest({ ...VALID_BODY, website: 'http://spam.example.com' })))

    expect(response.status).toBe(200)
  })

  it('does not call Mailjet when the honeypot website field is non-empty', async () => {
    await POST(makeContext(makeRequest({ ...VALID_BODY, website: 'http://spam.example.com' })))

    expect(mockRequest).not.toHaveBeenCalled()
  })

  it('returns 400 when name is missing from the request body', async () => {
    const response = await POST(makeContext(makeRequest({ ...VALID_BODY, name: '' })))

    expect(response.status).toBe(400)
  })

  it('returns 400 when name exceeds 200 characters', async () => {
    const response = await POST(makeContext(makeRequest({ ...VALID_BODY, name: 'a'.repeat(201) })))

    expect(response.status).toBe(400)
  })

  it('returns 400 when email is missing from the request body', async () => {
    const response = await POST(makeContext(makeRequest({ ...VALID_BODY, email: '' })))

    expect(response.status).toBe(400)
  })

  it('returns 400 when email is not a valid email address', async () => {
    const response = await POST(makeContext(makeRequest({ ...VALID_BODY, email: 'notanemail' })))

    expect(response.status).toBe(400)
  })

  it('returns 400 when message is missing from the request body', async () => {
    const response = await POST(makeContext(makeRequest({ ...VALID_BODY, message: '' })))

    expect(response.status).toBe(400)
  })

  it('returns 400 when message is fewer than 10 characters', async () => {
    const response = await POST(makeContext(makeRequest({ ...VALID_BODY, message: 'short' })))

    expect(response.status).toBe(400)
  })

  it('returns 400 when message exceeds 5000 characters', async () => {
    const response = await POST(makeContext(makeRequest({ ...VALID_BODY, message: 'a'.repeat(5001) })))

    expect(response.status).toBe(400)
  })

  it('returns a non-empty validation errors array when all required fields are absent', async () => {
    const response = await POST(makeContext(makeRequest({ name: '', email: '', message: '', website: '' })))
    const body = await response.json() as { errors: Array<{ field: string; message: string }> }

    expect(body.errors.length).toBeGreaterThan(0)
  })

  it('returns 500 when MJ_APIKEY_PUBLIC is not configured', async () => {
    setEnv({ MJ_APIKEY_PUBLIC: '' })

    const response = await POST(makeContext(makeRequest(VALID_BODY)))

    expect(response.status).toBe(500)
  })

  it('returns 500 when MJ_APIKEY_PRIVATE is not configured', async () => {
    setEnv({ MJ_APIKEY_PRIVATE: '' })

    const response = await POST(makeContext(makeRequest(VALID_BODY)))

    expect(response.status).toBe(500)
  })

  it('returns 500 when CONTACT_EMAIL is not configured', async () => {
    setEnv({ CONTACT_EMAIL: '' })

    const response = await POST(makeContext(makeRequest(VALID_BODY)))

    expect(response.status).toBe(500)
  })

  it('returns 500 when MAILJET_FROM_EMAIL is not configured', async () => {
    setEnv({ MAILJET_FROM_EMAIL: '' })

    const response = await POST(makeContext(makeRequest(VALID_BODY)))

    expect(response.status).toBe(500)
  })

  it('returns 200 when a valid submission is received and sendCopy is false', async () => {
    const response = await POST(makeContext(makeRequest(VALID_BODY)))

    expect(response.status).toBe(200)
  })

  it('calls Mailjet with exactly one message when sendCopy is false', async () => {
    await POST(makeContext(makeRequest(VALID_BODY)))

    const payload = mockRequest.mock.calls[0]?.[0] as { Messages: unknown[] }
    expect(payload.Messages).toHaveLength(1)
  })

  it('returns 200 when a valid submission is received and sendCopy is true', async () => {
    const response = await POST(makeContext(makeRequest({ ...VALID_BODY, sendCopy: true })))

    expect(response.status).toBe(200)
  })

  it('calls Mailjet with two messages when sendCopy is true', async () => {
    await POST(makeContext(makeRequest({ ...VALID_BODY, sendCopy: true })))

    const payload = mockRequest.mock.calls[0]?.[0] as { Messages: unknown[] }
    expect(payload.Messages).toHaveLength(2)
  })

  it('returns 500 when Mailjet throws during send', async () => {
    mockRequest.mockRejectedValue(new Error('Mailjet network failure'))

    const response = await POST(makeContext(makeRequest(VALID_BODY)))

    expect(response.status).toBe(500)
  })
})
