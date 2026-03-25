# Developer Reference (Supplementary)

Authoritative rules are in `.github/agents/Developer.agent.md` and SSOT docs.

## TDD Micro-Loop

1. Add one failing test
2. Prove failure
3. Minimal implementation
4. Run local target tests
5. Run full suite

## Quality Prompts

- Is behaviour explicit for happy/error/edge paths?
- Is async code cancellation-aware?
- Are boundaries respected and dependencies minimal?
- Are tests deterministic and behaviour-focused?

## Implementation Checklist

- Minimal focused diff
- No unrelated refactors
- Tests added/updated and passing
- Notes on assumptions and risk
