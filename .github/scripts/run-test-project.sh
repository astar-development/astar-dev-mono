#!/usr/bin/env bash
# usage: [TEST_RUNNER="xvfb-run -a"] run-test-project.sh <test project csproj>
# Runs one already-built test project and prints its output as one group, so several
# projects can run in parallel without their output being interleaved.
set -uo pipefail

proj="$1"
runner="${TEST_RUNNER:-}"
proj_dir=$(dirname "$proj")
proj_name=$(basename "$proj" .csproj)
workspace="${GITHUB_WORKSPACE:-$PWD}"

cd "$proj_dir" || exit 1

# shellcheck disable=SC2086
output=$($runner dotnet test \
  --no-build \
  --configuration Release \
  --results-directory "$workspace/TestResults" \
  --report-xunit-trx --report-xunit-trx-filename "$proj_name.trx" \
  --coverage --coverage-output-format cobertura --coverage-output "$proj_name.cobertura.xml" 2>&1)
status=$?

echo "::group::Tests: $proj_name (exit $status)"
echo "$output"
echo "::endgroup::"

if [ "$status" -ne 0 ]; then
  echo "::error::Tests failed in $proj_name"
fi

exit "$status"
