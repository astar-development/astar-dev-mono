---
description: "Use when adding or changing C# logging. Enforces LogMessage template usage, structured logging, and Azure Application Insights conventions."
name: "C# Logging Conventions"
applyTo: "**/*.cs"
---

# C# Logging Conventions

Use these rules whenever logging is added, changed, or reviewed in C# code.

## Core Rules

- All application logging MUST be compatible with Azure Application Insights.
- Logging MUST use existing LogMessage templates from AStar.Dev.Logging.Extensions when available.
- Direct logger.LogInformation, logger.LogWarning, logger.LogError, and string-interpolated log messages MUST NOT be introduced when a LogMessage template exists.
- If no suitable template exists, add a new LogMessage template and use it.

## Structured Logging

- Logs MUST be structured with stable property names.
- Logs MUST include domain identifiers needed for diagnosis and correlation.
- Logs MUST avoid high-cardinality or noisy payloads unless essential.

## Safety and Privacy

- Logs MUST NOT include secrets, tokens, credentials, or sensitive personal data.
- Exception logging MUST preserve stack and context while avoiding duplicate noise.

## Practical Guidance

- Use Information for expected business events, Warning for recoverable anomalies, and Error for failures requiring attention.
- Prefer one high-value log with context over many low-signal logs.
