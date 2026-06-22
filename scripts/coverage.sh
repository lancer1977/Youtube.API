#!/usr/bin/env bash
set -euo pipefail

usage() {
  cat <<'EOF'
Usage: coverage.sh <dotnet-test-target> [extra dotnet test args...]

Examples:
  COVERAGE_TARGET=PFAssistant.Core.Test/PFAssistant.Core.Test.csproj ./coverage.sh
  ./coverage.sh src/MyApp.Tests/MyApp.Tests.csproj --no-restore
EOF
}

extra_args=()
if [[ -n "${COVERAGE_TARGET:-}" ]]; then
  target="${COVERAGE_TARGET}"
  extra_args=("$@")
else
  target="${1:-}"
  if [[ -z "${target}" ]]; then
    usage
    exit 1
  fi
  shift || true
  extra_args=("$@")
fi

results_dir="${COVERAGE_RESULTS_DIR:-./TestResults/coverage}"
report_dir="${COVERAGE_REPORT_DIR:-${results_dir}/report}"
configuration="${CONFIGURATION:-Release}"

echo "Running coverage for: ${target}"
echo "Results directory: ${results_dir}"

dotnet test "${target}" \
  --configuration "${configuration}" \
  --collect:"XPlat Code Coverage" \
  --results-directory "${results_dir}" \
  "${extra_args[@]}"

coverage_file="$(find "${results_dir}" -name coverage.cobertura.xml -print -quit 2>/dev/null || true)"
if [[ -z "${coverage_file}" ]]; then
  echo "No coverage.cobertura.xml found under ${results_dir}"
  exit 0
fi

echo "Coverage XML: ${coverage_file}"

if command -v reportgenerator >/dev/null 2>&1; then
  mkdir -p "${report_dir}"
  reportgenerator \
    "-reports:${coverage_file}" \
    "-targetdir:${report_dir}" \
    "-reporttypes:Html;Cobertura"
  echo "HTML report: ${report_dir}/index.html"
else
  echo "ReportGenerator not found; skipping HTML report generation."
  echo "Install ReportGenerator or add a dotnet tool manifest in the owning repo to enable HTML output."
fi
