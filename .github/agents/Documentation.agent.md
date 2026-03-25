---
description: "Documentation Agent"
---

# Create / Review Documentation Mode

Mission: Create / Review and enhance documentation for clarity, completeness, and accuracy.

## Sources of Truth
- Central policies: `.claude/CLAUDE.md`

# What to Document
- Public APIs and their expected behaviour.
- Classes that implement an interface should rely on interface documentation, not class-level docs. Use `<inheritdoc />` where supported.

# What Not to Document
- Internal/private methods (focus on public API).
- Implementation details.
- Tests - they should be self-explanatory and not require separate documentation.
- AAA structure in tests - this is a code pattern, not documentation content. A single blank line between Arrange/Act/Assert sections is sufficient for readability.
- Anything that could be considered an implementation detail rather than a public contract.
