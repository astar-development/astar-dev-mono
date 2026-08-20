#!/usr/bin/env bash
set -euo pipefail

find . -type d -name TestResults -not -path '*/node_modules/*' -exec rm -rf {} +
rm -rf CoverageReport

dotnet clean --nologo
dotnet build --no-restore --nologo

dotnet test --no-build --collect:"XPlat Code Coverage" -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura || true

reportgenerator -reports:**/TestResults/**/coverage.cobertura.xml -targetdir:CoverageReport
