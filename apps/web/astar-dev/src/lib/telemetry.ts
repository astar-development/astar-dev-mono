import appInsights from 'applicationinsights';

type SeverityLevel = 0 | 1 | 2 | 3 | 4;

const Severity = {
  Verbose: 0 as SeverityLevel,
  Information: 1 as SeverityLevel,
  Warning: 2 as SeverityLevel,
  Error: 3 as SeverityLevel,
  Critical: 4 as SeverityLevel,
};

function initClient(): appInsights.TelemetryClient | null {
  const connectionString = process.env.APPLICATIONINSIGHTS_CONNECTION_STRING;
  if (typeof connectionString !== 'string' || connectionString.length === 0) {
    return null;
  }

  if (!appInsights.defaultClient) {
    appInsights
      .setup(connectionString)
      .setAutoCollectRequests(false)
      .setAutoCollectExceptions(true)
      .setAutoCollectDependencies(true)
      .setAutoCollectConsole(false)
      .start();
  }

  return appInsights.defaultClient;
}

const client: appInsights.TelemetryClient | null = initClient();

function getClient(): appInsights.TelemetryClient | null {
  return client;
}

export function trackTrace(message: string, properties?: Record<string, string>, severity: SeverityLevel = Severity.Information): void {
  getClient()?.trackTrace({ message, properties, severity });
}

export function trackWarning(message: string, properties?: Record<string, string>): void {
  getClient()?.trackTrace({ message, properties, severity: Severity.Warning });
}

export function trackException(error: Error, properties?: Record<string, string>): void {
  getClient()?.trackException({ exception: error, properties });
}

export function trackEvent(name: string, properties?: Record<string, string>): void {
  getClient()?.trackEvent({ name, properties });
}
