---
title: 'Distributed Pipeline Rebuild'
summary: 'Replaced a fragile monolithic batch process with an event-driven architecture, cutting deployment risk and halving on-call incidents.'
techStack: ['.NET 7', 'Azure Functions', 'Cosmos DB', 'Application Insights']
---

## The Problem

A logistics technology company was running a nightly batch job that consolidated records from six upstream systems into a single reporting database. The job had grown to over four hours in execution time, had no retry logic, and a single failure anywhere in the sequence would invalidate the entire night's run. Deployments required a maintenance window because the monolith held shared state that could not be safely updated while the batch was running. On-call engineers were waking up to failures two to three times a week.

## The Approach

The batch process was decomposed into a set of independently deployable Azure Functions, each responsible for a single upstream system. Events were published to a shared topic in Cosmos DB change-feed format, allowing downstream consumers to process records as they arrived rather than waiting for an overnight window. Each function was designed to be idempotent, with structured retry policies and a dead-letter path that preserved failed records for inspection without halting the rest of the pipeline. Application Insights correlation IDs tied every step back to the originating source record.

## The Outcome

The nightly four-hour batch was replaced by a continuous pipeline that keeps reporting data within fifteen minutes of the upstream source at all times. Deployments became zero-downtime by default, as individual functions can be updated independently without coordinating a maintenance window. On-call incident volume halved within the first month, and the remaining pages were all handled within minutes rather than requiring multi-hour investigations. The team reported a significant reduction in deployment anxiety and an improvement in confidence when pushing changes.
