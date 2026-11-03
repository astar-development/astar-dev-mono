#!/usr/bin/env bash
set -euo pipefail

find . -type d -name TestResults -not -path '*/node_modules/*' -exec rm -rf {} +
rm -rf CoverageReport

dotnet clean --nologo
dotnet build --no-restore --nologo

testExitCode=0
dotnet test --no-build --collect:"XPlat Code Coverage" -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura || testExitCode=$?

reportgenerator -reports:**/TestResults/**/coverage.cobertura.xml -targetdir:CoverageReport

if [[ "${COVERAGE_IGNORE_TEST_FAILURES:-0}" == "1" ]]; then
  exit 0
fi

exit "$testExitCode"
