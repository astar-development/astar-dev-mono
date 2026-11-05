# Review subagent prompt (issue-to-pr step 7)

Fill in the `<...>` placeholders and send this text as the `prompt` of an `Agent` call with `subagent_type: general-purpose`.

---

You are a senior C# reviewer. Review branch `<branch>` in the git worktree `<worktree path>` against GitHub issue #<n> (`gh issue view <n>`). Diff: `git diff origin/main...HEAD`.

Do NOT edit files, commit, switch branches or stash. Report only. You may build and test inside the worktree: `rtk bypass dotnet build --no-restore <proj>`, then `dotnet test --no-build --project <dir>`.

Owner decisions already made (do not flag them as problems):
<bullet list of the decisions, including anything deferred to other issues>

Check:
1. Each acceptance point in the issue: met, not met or deferred (according to the decisions above), with file:line.
2. Correctness:
   - cancellation (user cancel versus timeout);
   - rollback and cleanup paths;
   - thread safety and running once;
   - error mapping;
   - DI lifetimes (singletons that capture transient or scoped objects, HttpClient use);
   - options binding;
   - disposal;
   - third-party API misuse.
3. No PII in logs (LG-02): no emails, names, tokens, account keys, file paths, raw exception messages or exception objects.
4. Repo rules in `CLAUDE.md` and `.claude/rules/c-sharp-code-style.md`:
   - no try/catch; use `Try.Run`/`Try.RunAsync`;
   - no null returns; use `Option`;
   - no bool or enum for state; use unions;
   - records have factories;
   - XML docs on public members, and `<inheritdoc />` on interface implementations;
   - no comments inside methods, and none at all in test files;
   - test names `Given*`/`when_*_then_*`, assertions with Shouldly;
   - a `CancellationToken` passed everywhere;
   - no nested ternaries;
   - no services created with `new` inside a class.
5. Coverage risk: new code that only integration tests reach, when Sonar skips that project, and logic sitting inside `[ExcludeFromCodeCoverage]` classes.
6. Test gaps that matter for the story: concurrency, casing variants, cancellation, rollback, PII.

Output, under 600 words: findings ranked by severity, each with file:line, the problem, a concrete failure scenario and a suggested fix. List separately anything that needs an owner decision rather than a code fix.
