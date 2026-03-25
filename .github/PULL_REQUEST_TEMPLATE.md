# Summary

Please include a brief description of the change and the motivation.

## Type of change
- [ ] Feature
- [ ] Bug fix
- [ ] Refactor
- [ ] CI/CD
- [ ] Documentation
- [ ] Maintenance

## Related issues / links
<!-- e.g., Closes #123, Relates to #456 -->

## How was this tested?
- [ ] Unit tests added/updated
- [ ] Manual validation
- [ ] Not applicable

## Impacted areas
<!-- e.g., libs/MyLib, apps/MyApp, web/Api -->

---

## TDD Checklist (required)

- [ ] Builds locally
- [ ] Tests pass (`dotnet test`)
- [ ] No new analyzer warnings
- [ ] Public API changes reviewed
- [ ] Documentation updated (if applicable)
- [ ] The failing-test commit is included in this branch history (the failing test must be present in the PR history before or alongside production code).

If the TDD checklist is not applicable for this PR (e.g., documentation-only or chore), explain why in the description.

---

## How to run tests locally

```bash
# Restore and run tests
dotnet restore AStar.Dev.slnx
dotnet test --verbosity normal
```

---

## Notes for reviewers

- Verify that the PR history includes the failing-test commit (or an explanation why not).
- Ensure CI passed for all OS runners.
