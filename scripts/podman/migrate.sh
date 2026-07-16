#!/usr/bin/env bash

set -Eeuo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=_common.sh
source "${SCRIPT_DIR}/_common.sh"

load_environment
ensure_podman_available
require_sqlserver_running
require_command dotnet

export INVOICE_DATABASE_CONNECTION="Server=127.0.0.1,${PODMAN_SQL_PORT};Database=InvoiceManagement;User Id=sa;Password=${SQL_SA_PASSWORD};Encrypt=True;TrustServerCertificate=True;Connect Timeout=60"
export ConnectionStrings__InvoiceDatabase="${INVOICE_DATABASE_CONNECTION}"

cd "${REPOSITORY_ROOT}"
dotnet tool restore
dotnet restore InvoiceManagement.sln
dotnet ef database update \
    --project src/InvoiceManagement.Infrastructure \
    --startup-project src/InvoiceManagement.Api

printf 'EF Core migrations were applied to localhost:%s.\n' "${PODMAN_SQL_PORT}"
