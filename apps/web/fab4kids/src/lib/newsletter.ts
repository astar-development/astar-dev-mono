import { existsSync, mkdirSync, readFileSync, writeFileSync } from 'fs';
import { dirname, resolve } from 'path';

interface Subscriber {
  email: string;
  subscribedAt: string;
}

const DATA_FILE = resolve(process.cwd(), 'data/newsletter-subscribers.json');

function readSubscribers(): Subscriber[] {
  if (!existsSync(DATA_FILE)) return [];
  try {
    const raw = readFileSync(DATA_FILE, 'utf-8');

    return JSON.parse(raw) as Subscriber[];
  } catch {
    return [];
  }
}

function writeSubscribers(subscribers: Subscriber[]): void {
  mkdirSync(dirname(DATA_FILE), { recursive: true });
  writeFileSync(DATA_FILE, JSON.stringify(subscribers, null, 2), 'utf-8');
}

export function hasSubscriber(email: string): boolean {
  const normalised = email.toLowerCase();

  return readSubscribers().some((s) => s.email.toLowerCase() === normalised);
}

export function addSubscriber(email: string): 'added' | 'duplicate' {
  if (hasSubscriber(email)) return 'duplicate';
  const subscribers = readSubscribers();
  subscribers.push({ email: email.toLowerCase(), subscribedAt: new Date().toISOString() });
  writeSubscribers(subscribers);

  return 'added';
}
