# Contributing to AStar Dev repo

Thank you for your interest in contributing! This document provides guidelines for contributing to the project.

---

## Code Review Checklist

Before submitting a pull request, ensure your code meets these requirements:

### Code Quality

- [ ] Code follows naming conventions and style guide ([style-guidelines](instructions/style-guidelines.instructions.md))
- [ ] All public members have XML documentation comments
- [ ] No warnings (project has `TreatWarningsAsErrors = true`)
- [ ] Code is properly formatted (use IDE formatter)

### Architecture & Design

- [ ] New services/repositories have interfaces for testability
- [ ] Business logic is abstracted behind interfaces (no `new` instantiation of services)
- [ ] Dependencies injected via constructor parameters
- [ ] Follows layered architecture (Core → Infrastructure → Presentation)

### Testing

- [ ] TDD workflow followed (failing test committed in branch history)
- [ ] Tests cover happy path and error cases
- [ ] Unit tests for business logic (80%+ coverage)
- [ ] Integration tests for cross-service flows (where applicable)
- [ ] All existing tests pass locally

### Database Changes

- [ ] Database changes have Entity Framework migrations
- [ ] Migration reviewed and tested
- [ ] Migration includes `Up()` and `Down()` methods for rollback

### Async/Concurrency

- [ ] Async/await used throughout (no `Task.Wait()` or `Task.Result`)
- [ ] `CancellationToken` parameters added to async methods
- [ ] Proper disposal of resources (`using` statements)

### Documentation

- [ ] README updated (if applicable)
- [ ] API documentation added for public interfaces
- [ ] Comments explain "why", not "what" (code should be self-explanatory)

---

## Commit Message Style

Use conventional commits format for all commit messages. This enables automatic changelog generation and semantic versioning.

### Format

```
<type>(<scope>): <subject>

<body>

<footer>
```

### Types

- **feat**: New feature
- **fix**: Bug fix
- **refactor**: Code refactoring (no functional changes)
- **test**: Adding or updating tests
- **docs**: Documentation changes
- **chore**: Build process, dependencies, tooling
- **perf**: Performance improvements
- **style**: Code style changes (formatting, whitespace)

### Examples

```
feat(sync): add delta query support for incremental sync

Implement delta query pattern using Microsoft Graph API /delta endpoint.
This enables efficient incremental synchronization by fetching only changed
items since last sync.

Closes #123
```

```
fix(auth): handle token expiration gracefully

Catch token expiration and prompt re-authentication instead of crashing.
Added retry logic with exponential backoff for transient failures.

Fixes #456
```

```
refactor(repositories): extract common repository logic to base class

Extract shared CRUD operations to AbstractRepository base class to reduce
duplication across repositories. No functional changes.
```

### Best Practices

- Use imperative mood: "add" not "added" or "adds"
- Don't capitalize first letter of subject
- No period at end of subject
- Limit subject line to 50 characters
- Wrap body at 72 characters
- Reference issues and PRs in footer

---

## GitHub Pull Request Creation

## Workflow Requirements

All changes must go through pull requests. Direct commits to main branch are blocked by branch protection rules.

### Creating a Pull Request

**1. Create feature branch**:
```bash
git checkout -b feature/your-feature-name
```

**2. Make changes following TDD**:
```bash
# Write failing test
# Commit failing test
git add .
git commit -m "test: add test for new feature (failing)"

# Implement feature
# Commit passing implementation
git add .
git commit -m "feat: implement new feature"

# Refactor if needed
git add .
git commit -m "refactor: improve feature implementation"
```

**3. Push branch**:
```bash
git push -u origin feature/your-feature-name
```

**4. Create PR via GitHub API** (preferred method):

Use the GitHub Copilot tool:
```text
{
  owner: "astar-development",
  repo: "astar-dev-mono",
  title: "feat: descriptive title using conventional commits",
  head: "feature/your-feature-name",
  base: "main",
  body: `## Summary

Brief description of changes and rationale.

## Changes

- Change 1: Description
- Change 2: Description
- Change 3: Description

## Testing

- [ ] Unit tests added/updated
- [ ] Integration tests pass
- [ ] Manual testing completed
- [ ] No warnings or errors

## Checklist

- [ ] Code follows TDD workflow (failing test committed)
- [ ] Code follows style guidelines
- [ ] All tests pass locally
- [ ] Documentation updated (if needed)
- [ ] No database changes OR migrations included

## Related Issues

Closes #123
Relates to #456
  `
}
```

**Why use the API?** GitHub does not support URL query parameters for pre-filling PR title/description. The `github.com/owner/repo/pull/new/branch?title=...&body=...` pattern does not work.

### Pull Request Guidelines

## Workflow Requirements

- **PR Size**: < 300 lines of code changed, < 20 files (break down larger changes)
- **Review Time**: Respond to reviewer feedback within 12 hours
- **Merge Time**: PRs reviewed and merged within 24 hours (when possible)
- **Approval**: At least one approval required before merge
- **CI Gates**: All tests pass, no warnings, linter clean
- **Merge Strategy**: Squash and merge for clean history

### Large Changes / Refactors

For changes that exceed PR size limits:

1. **Use Feature Flags**: Protect work behind disabled feature flag
2. **Incremental PRs**: Break work into smaller, independent PRs
3. **Draft PRs**: Use draft PRs for early feedback and work-in-progress
4. **Communicate**: Add detailed description explaining scope and approach

**Feature Flag Example**:

```csharp
public class FeatureFlags
{
    public bool EnableNewSyncAlgorithm { get; set; } = false;  // Disabled by default
}

public async Task SyncAsync()
{
    if (_featureFlags.EnableNewSyncAlgorithm)
    {
        await NewSyncAlgorithmAsync();  // New code path
    }
    else
    {
        await LegacySyncAsync();  // Existing code path
    }
}
```

---

## Branching Strategy

## Naming Strategies

- **Feature branches**: `feature/<descriptive-name>` (e.g., `feature/add-file-watcher`)
- **Bug fixes**: `fix/<descriptive-name>` (e.g., `fix/resolve-sync-conflict`)
- **Refactors**: `refactor/<descriptive-name>` (e.g., `refactor/extract-interfaces`)
- **Documentation**: `docs/<descriptive-name>` (e.g., `docs/update-readme`)

### Trunk-Based Development

- **Main branch**: Always deployable, all tests passing
- **Short-lived branches**: Feature branches merged within 1-2 days
- **Small changes**: Prefer small, frequent merges over large, infrequent ones
- **Feature flags**: Use for incomplete features to enable continuous integration

### Branch Lifetime

1. Create branch from latest `main`
2. Make changes following TDD workflow
3. Push branch and create PR
4. Address review feedback
5. Merge to `main` via "Squash and Merge"
6. Delete branch automatically after merge

---

## Getting Help

- **Questions**: Open a GitHub Discussion
- **Bugs**: Open a GitHub Issue with reproduction steps
- **Features**: Open a GitHub Issue with use case and requirements
- **Security**: Email security@astar.dev (do not open public issue)

---

## Code of Conduct

- Be respectful and constructive in all interactions
- Focus on the code and ideas, not the person
- Welcome newcomers and help them get started
- Assume good intent
- Report unacceptable behavior to maintainers

---

## Additional Resources

- [Style Guidelines](instructions/style-guidelines.instructions.md)
- [Development Tasks](instructions/development-tasks.instructions.md)
- [Troubleshooting](instructions/troubleshooting.instructions.md)
- [Performance Guidelines](instructions/performance.instructions.md)
- [Architecture Documentation](../docs/README.md)
