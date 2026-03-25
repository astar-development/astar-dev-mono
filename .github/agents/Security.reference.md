# Security Reference (Supplementary)

Authoritative rules are in `.github/agents/Security.agent.md` and SSOT docs.

## Threat Modeling Mini-Checklist

- Assets and sensitive data
- Actors and trust boundaries
- Entry points and attack surface
- Abuse cases and likely threats
- Existing controls and gaps

## Security Review Prompts

- Are authn/authz checks complete and explicit?
- Is untrusted input validated and encoded/sanitized appropriately?
- Are secrets protected in code, config, and workflows?
- Are logs safe and useful for forensics?

## Severity Guide (Use SSOT taxonomy)

- Blocking: exploitable or policy-critical risk
- Recommended: meaningful risk reduction
- Nit: minor hardening/readability improvement
