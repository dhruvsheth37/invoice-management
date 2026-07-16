#!/usr/bin/env bash

set -Eeuo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=_common.sh
source "${SCRIPT_DIR}/_common.sh"

load_environment
ensure_podman_available
require_sqlserver_running

compose exec --no-TTY sqlserver sh -c \
    '/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -b -d InvoiceManagement' \
    < "${REPOSITORY_ROOT}/database/seed/SeedDemoData.sql"

printf 'Demo data was loaded successfully.\n'
