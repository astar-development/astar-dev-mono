# DevOps Reference (Supplementary)

Authoritative rules are in `.github/agents/DevOps.agent.md` and SSOT docs.

## Pipeline Review Checklist

- Job-level least-privilege permissions
- Deterministic build and test steps
- Security scanning gates
- Artifact provenance and retention
- Rollback strategy documented

## Deployment Safety Prompts

- What is the blast radius?
- How is health validated?
- What is the rollback trigger and mechanism?

## Observability Minimums

- Structured logs
- Deployment/change markers
- Alerting on critical failures
