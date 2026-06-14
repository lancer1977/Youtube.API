#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
WORKSPACE_DIR="$ROOT_DIR"
CONFIGURATION="${CONFIGURATION:-Release}"
SOLUTION_FILE="${SOLUTION_FILE:-APi.Youtube.sln}"
PROJECT_PATH="${PROJECT_PATH:-Src/PolyhydraGames.APi.Youtube.csproj}"
TEST_PROJECT_PATH="${TEST_PROJECT_PATH:-test/Test.csproj}"
PACKAGES_DIR="${PACKAGES_DIR:-$ROOT_DIR/artifacts/package}"
PACKAGE_VERSION="${PACKAGE_VERSION:-}"
PUBLISH_GITHUB_PACKAGES="${PUBLISH_GITHUB_PACKAGES:-true}"
PACKAGE_SOURCE="${PACKAGE_SOURCE:-https://nuget.pkg.github.com/${GITHUB_REPOSITORY_OWNER:-lancer1977}/index.json}"
PACKAGE_API_KEY="${PACKAGE_API_KEY:-${GHCR_TOKEN:-${GITHUB_PACKAGES_TOKEN:-${GITHUB_TOKEN:-${GH_TOKEN:-}}}}}"
PUBLISH_NUGET_ORG="${PUBLISH_NUGET_ORG:-false}"
NUGET_ORG_SOURCE="${NUGET_ORG_SOURCE:-https://api.nuget.org/v3/index.json}"
NUGET_ORG_API_KEY="${NUGET_ORG_API_KEY:-${NUGET_API_KEY:-}}"
DRY_RUN="${DRY_RUN:-false}"

show_help() {
  cat <<'EOF'
Usage: ./pub.sh

Builds, tests, packs, and optionally publishes the YouTube package.

Environment:
  SOLUTION_FILE              Solution to restore/build/test.
  PROJECT_PATH               Package project path.
  TEST_PROJECT_PATH          Test project path.
  PACKAGES_DIR               Output directory for .nupkg files.
  PACKAGE_VERSION            Override package version.
  PUBLISH_GITHUB_PACKAGES    Set to true to push packages to GitHub Packages.
  PACKAGE_API_KEY            API key for GitHub Packages push.
  GHCR_TOKEN / GITHUB_PACKAGES_TOKEN / GITHUB_TOKEN / GH_TOKEN
                             Fallback token for package push.
  PUBLISH_NUGET_ORG          Set to true to push packages to nuget.org.
  NUGET_ORG_API_KEY          API key for nuget.org push.
  DRY_RUN                    Set to true to skip push steps.
EOF
}

case "${1:-}" in
  -h|--help)
    show_help
    exit 0
    ;;
esac

determine_version() {
  if [[ -n "$PACKAGE_VERSION" ]]; then
    printf '%s\n' "$PACKAGE_VERSION"
    return
  fi

  cd "$WORKSPACE_DIR"
  if git describe --tags --abbrev=0 >/dev/null 2>&1; then
    git describe --tags --abbrev=0 | sed 's/^v//'
    return
  fi

  printf '0.0.%s\n' "$(git rev-list --count HEAD)"
}

VERSION="$(determine_version)"

mkdir -p "$PACKAGES_DIR"

cd "$WORKSPACE_DIR"
dotnet restore "$TEST_PROJECT_PATH"
dotnet build "$TEST_PROJECT_PATH" --configuration "$CONFIGURATION" --no-restore
dotnet test "$TEST_PROJECT_PATH" --configuration "$CONFIGURATION" --no-restore --no-build --verbosity normal

rm -f "$PACKAGES_DIR"/*.nupkg 2>/dev/null || true
dotnet pack "$PROJECT_PATH" \
  --configuration "$CONFIGURATION" \
  --no-build \
  -p:PackageVersion="$VERSION" \
  --output "$PACKAGES_DIR"

echo "Packed YouTube artifacts to $PACKAGES_DIR"

if [[ "$PUBLISH_GITHUB_PACKAGES" == "true" || "$PUBLISH_GITHUB_PACKAGES" == "1" ]]; then
  if [[ "$DRY_RUN" == "true" || "$DRY_RUN" == "1" ]]; then
    echo "DRY_RUN: dotnet nuget push \"$PACKAGES_DIR\"/*.nupkg --source \"$PACKAGE_SOURCE\" --api-key *** --skip-duplicate"
  else
    if [[ -z "$PACKAGE_API_KEY" ]]; then
      echo "PUBLISH_GITHUB_PACKAGES is enabled, but PACKAGE_API_KEY/GITHUB_TOKEN/GH_TOKEN is not set." >&2
      exit 1
    fi

    for package in "$PACKAGES_DIR"/*.nupkg; do
      dotnet nuget push "$package" \
        --source "$PACKAGE_SOURCE" \
        --api-key "$PACKAGE_API_KEY" \
        --skip-duplicate
    done
  fi
fi

if [[ "$PUBLISH_NUGET_ORG" == "true" || "$PUBLISH_NUGET_ORG" == "1" ]]; then
  if [[ "$DRY_RUN" == "true" || "$DRY_RUN" == "1" ]]; then
    echo "DRY_RUN: dotnet nuget push \"$PACKAGES_DIR\"/*.nupkg --source \"$NUGET_ORG_SOURCE\" --api-key *** --skip-duplicate"
  else
    if [[ -z "$NUGET_ORG_API_KEY" ]]; then
      echo "PUBLISH_NUGET_ORG is enabled, but NUGET_ORG_API_KEY/NUGET_API_KEY is not set." >&2
      exit 1
    fi

    for package in "$PACKAGES_DIR"/*.nupkg; do
      dotnet nuget push "$package" \
        --source "$NUGET_ORG_SOURCE" \
        --api-key "$NUGET_ORG_API_KEY" \
        --skip-duplicate
    done
  fi
fi

echo "YouTube publish helper complete."
