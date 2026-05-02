## Definition of Done

Before marking task complete:

1. **Create feature branch** (never on main)
2. **TDD: write failing test first**, commit before production code
3. **Build:** `dotnet build` affected projects—zero errors, zero warnings
4. **Test:** `dotnet test` affected test projects—all pass
5. **Logging:** all logging → Azure Application Insights via `AStar.Dev.Logging.Extensions` (compile-time `LogMessage` only)
6. **DI:** language supports it? must implement from start
7. **Code style:** Follow c-sharp-code-style.md
8. **No redundant comments:** Extract methods instead
9. **Request human review** before committing
10. **Implement feedback if requested**
11. **Commit with conventional message** to branch (only after approval)
12. **Raise GitHub PR**

## Key Principles

- NEVER afterthought: logging + DI
- Extract sub-methods for SRP violations
- No magic strings/numbers—use constants/enums
- Immutable-first for data
- Factory pattern for records
