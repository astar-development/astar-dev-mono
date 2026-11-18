#!/usr/bin/env bash
# Usage: ./publish-tag.sh <PackageName> <version> [--replace]
# Example: ./publish-tag.sh AStarDev.InfrastructureAppDb 2.1.2
# Creates and pushes tag <PackageName>/v<version>, which triggers nuget-publish.yml.
# --replace deletes an existing local+remote tag of the same name first.
set -euo pipefail

package="${1:-}"
version="${2:-}"
replace="${3:-}"

if [ -z "$package" ] || [ -z "$version" ]; then
  echo "Usage: $0 <PackageName> <version> [--replace]" >&2
  exit 1
fi

package="${package%.csproj}"
version="${version#v}"

if ! [[ "$version" =~ ^[0-9]+\.[0-9]+\.[0-9]+(-.+)?$ ]]; then
  echo "ERROR: version '$version' is not x.y.z or x.y.z-suffix" >&2
  exit 1
fi

cd "$(git rev-parse --show-toplevel)"

if [ -z "$(find packages -name "${package}.csproj" -print -quit)" ]; then
  echo "ERROR: no ${package}.csproj under packages/ (name is case-sensitive)" >&2
  exit 1
fi

tag="${package}/v${version}"

if git rev-parse -q --verify "refs/tags/${tag}" >/dev/null || git ls-remote --exit-code --tags origin "refs/tags/${tag}" >/dev/null 2>&1; then
  if [ "$replace" != "--replace" ]; then
    echo "ERROR: tag ${tag} already exists. Re-run with --replace to delete and recreate it." >&2
    exit 1
  fi
  git tag -d "$tag" 2>/dev/null || true
  git push origin ":refs/tags/${tag}"
fi

git tag "$tag"
git push origin "$tag"
echo "Pushed ${tag}"
