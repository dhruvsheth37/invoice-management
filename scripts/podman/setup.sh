#!/usr/bin/env bash

set -Eeuo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"

"${SCRIPT_DIR}/start.sh"
"${SCRIPT_DIR}/migrate.sh"
"${SCRIPT_DIR}/seed.sh"

printf '\nPodman SQL Server, schema, and demo data are ready.\n'
printf 'Start the API with ./scripts/podman/run-api.sh\n'
