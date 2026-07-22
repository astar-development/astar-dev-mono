import appInsights, { KnownSeverityLevel } from 'applicationinsights';

type SeverityLevel = KnownSeverityLevel;

const Severity = {
  Verbose: KnownSeverityLevel.Verbose,
  Information: KnownSeverityLevel.Information,
  Warning: KnownSeverityLevel.Warning,
  Error: KnownSeverityLevel.Error,
  Critical: KnownSeverityLevel.Critical,
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
