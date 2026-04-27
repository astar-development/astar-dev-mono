---
name: add-tests
description: Add behaviour tests to a C# file via the c-sharp-qa subagent. Reads the target file and its dependencies, captures a baseline test count, delegates test writing to the QA agent, then reports the delta and any pre-existing failures.
---

Add behaviour tests to the C# file specified in $ARGUMENTS.

**Input**: Path to the target `.cs` source file (not the test file). Example: `/add-tests apps/desktop/Foo/Services/BarService.cs`

**Steps**

1. **Resolve the target file**

   If $ARGUMENTS is a relative path, resolve it against the repo root. If no argument is given, ask the user which file to test.

2. **Capture baseline test results**

   Run the full test suite and capture the output before any changes:

   ```bash
   dotnet test --nologo 2>&1
   ```

   Parse and record:
   - Total tests passed (baseline count)
   - Any already-failing tests (name + error) — label these **pre-existing failures**

   If the build itself fails, report that immediately and stop.

3. **Read the target file and its direct dependencies**

   - Read the target source file in full.
   - Identify every type it directly depends on (constructor parameters, method parameters, return types, field types).
   - Read each dependency file. Use `Glob` / `Grep` to locate them if the path is not obvious.
   - Locate the existing test project for this file (pattern: `[Assembly].Tests.Unit`) and read any existing test class for this type so the agent does not duplicate coverage.
   - Read `.claude/rules/c-sharp-code-style.md` and `.claude/agents/c-sharp-qa.md` for conventions.

4. **Spawn the c-sharp-qa subagent**

   Delegate with this briefing (fill in the placeholders):

   > You are writing behaviour tests for `{TARGET_FILE}` in the AStar.Dev mono-repo.
   >
   > **Target file content:**
   > {TARGET_FILE_CONTENT}
   >
   > **Direct dependencies (read these before writing tests):**
   > {LIST_OF_DEPENDENCY_PATHS}
   >
   > **Existing test class (if any) — do not duplicate these cases:**
   > {EXISTING_TEST_CLASS_CONTENT_OR_"None"}
   >
   > **Conventions:**
   > - Test class: `Given[Context]`, sealed, in `[Assembly].Tests.Unit`
   > - Test methods: `when_[action]_then_[outcome]` snake_case
   > - Assertions: Shouldly only — never `Assert.*`
   > - Mocking: NSubstitute only
   > - Global usings already cover xUnit, Shouldly, NSubstitute — do not re-add them
   > - No comments, no XML docs inside test classes
   > - AAA sections separated by a single blank line; no `// Arrange` labels
   > - Single-line method signatures regardless of parameter count
   > - Blank line before every `return` statement except when the return follows `if`/`else` directly
   >
   > Write tests covering all branches and observable behaviours not already covered.
   > After writing, run `dotnet build` on the test project and confirm zero errors and zero warnings.
   > Do NOT run `dotnet test` — the caller will do that.

5. **Run the test suite after the agent completes**

   ```bash
   dotnet test --nologo 2>&1
   ```

   Parse:
   - New total passed count
   - Any newly failing tests (tests that were passing before but now fail)

6. **Report results**

   Output a summary in this format:

   ```
   ## Add Tests: {TARGET_FILE_NAME}

   ### Test Count
   Before: {BASELINE_COUNT} passing
   After:  {NEW_COUNT} passing
   Delta:  +{DELTA} new tests

   ### Pre-existing Failures (not caused by this change)
   {LIST_OR_"None"}

   ### New Failures Introduced
   {LIST_OR_"None — all tests pass"}
   ```

   If new failures were introduced, do NOT commit. Investigate and fix before reporting done.

**Guardrails**
- Never write tests directly yourself — always delegate to the c-sharp-qa subagent.
- Never commit or create a PR — the human decides when to commit.
- If the test project does not exist, stop and ask the human before proceeding.
- If baseline has build errors, stop immediately and report them.
- Pre-existing failures are informational only; do not attempt to fix them unless asked.
