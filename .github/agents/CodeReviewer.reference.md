# Code Reviewer Reference (Supplementary)

This file provides optional depth and examples.
Authoritative reviewer rules remain in:

- `.github/agents/CodeReviewer.agent.md`
- `docs/engineering/code-review-guidelines.md`
- `.claude/CLAUDE.md`

## Compact Review Checklist

- Requirements implemented correctly
- Edge/error paths handled
- Architecture boundaries respected
- Async/cancellation patterns correct
- Data-access patterns efficient
- Tests deterministic and behaviour-focused
- Policy/standards alignment

## Finding Template

Use per finding:

- Severity: `blocking | recommended | nit`
- Issue: concise problem statement
- Impact: risk/consequence
- Evidence: file/symbol/context
- Fix: concrete next step

## Common Blocking Findings

- Broken correctness or regression risk
- Security/privacy/compliance violation
- Architecture boundary breach
- Critical missing tests on changed critical paths
- Policy violations from SSOT guidelines

## Common Recommended Findings

- Improve readability/maintainability
- Strengthen non-critical test scenarios
- Reduce complexity/duplication
- Improve error messaging/observability

## Common Nits

- Naming consistency
- Minor simplifications
- Style cleanup better handled by linters/formatters

## C#/.NET Focus Prompts

- Any sync-over-async risk?
- Cancellation token propagated?
- `Result`/`Option` usage aligned to policy?
- Any potential N+1 or heavy materialization?
- Any disposable/subscription lifetime leaks?

## Positive Feedback Prompt

Always include at least one positive note, such as:

- clear test naming
- good dependency direction
- robust error-path coverage
- improved readability with small focused methods
