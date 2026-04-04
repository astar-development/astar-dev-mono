// In-memory store — resets on process restart.
// Post-MVP: replace with a persistent store (Redis or DB) to survive restarts
// and handle multiple App Service instances.
const processed = new Set<string>();

export function isAlreadyProcessed(sessionId: string): boolean {
  return processed.has(sessionId);
}

export function markAsProcessed(sessionId: string): void {
  processed.add(sessionId);
}
