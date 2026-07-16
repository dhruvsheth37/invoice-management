#!/usr/bin/env bash

set -Eeuo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=_common.sh
source "${SCRIPT_DIR}/_common.sh"

load_environment
ensure_podman_available
require_sqlserver_running
require_command dotnet

export ConnectionStrings__InvoiceDatabase="Server=127.0.0.1,${PODMAN_SQL_PORT};Database=InvoiceManagement;User Id=sa;Password=${SQL_SA_PASSWORD};Encrypt=True;TrustServerCertificate=True;Connect Timeout=60"

cd "${REPOSITORY_ROOT}"
printf 'Starting the API with Podman SQL Server on localhost:%s...\n' "${PODMAN_SQL_PORT}"
exec dotnet run --project src/InvoiceManagement.Api
