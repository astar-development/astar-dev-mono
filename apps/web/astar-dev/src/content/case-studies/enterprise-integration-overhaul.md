---
title: 'Enterprise Integration Overhaul'
summary: 'Reduced message-processing latency by 60% across a distributed .NET 8 pipeline serving 50M+ daily events.'
techStack: ['.NET 8', 'Azure Service Bus', 'SQL Server', 'OpenTelemetry']
---

## The Problem

A financial services client was processing north of 50 million events per day through a brittle integration layer built incrementally over several years. Message-processing latency had crept above acceptable thresholds, with p99 times regularly exceeding 8 seconds and frequent dead-letter queue spikes causing downstream reconciliation failures. The existing architecture had no structured observability, making root-cause analysis time-consuming and largely guesswork.

## The Approach

An audit of the existing pipeline identified three compounding issues: synchronous database writes inside the hot path, unbounded fan-out on Azure Service Bus topics, and missing back-pressure controls that allowed slow consumers to stall the entire chain. The solution introduced a dedicated staging layer with explicit partition strategies, replaced inline writes with batched, idempotent SQL Server upserts, and instrumented every stage with OpenTelemetry traces and metrics. A phased cut-over allowed validation under production load without a big-bang migration.

## The Outcome

End-to-end p99 latency dropped from over 8 seconds to under 3 seconds — a 60% reduction — within the first two weeks of the new pipeline running at full volume. Dead-letter queue incidents fell to near zero, and the new OpenTelemetry dashboards gave the operations team genuine visibility into throughput and error rates for the first time. The client's on-call rotation reported a measurable reduction in overnight pages within the first month.
