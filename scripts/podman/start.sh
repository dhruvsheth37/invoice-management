#!/usr/bin/env bash

set -Eeuo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=_common.sh
source "${SCRIPT_DIR}/_common.sh"

load_environment
ensure_podman_available

printf 'Starting SQL Server with Podman on localhost:%s...\n' "${PODMAN_SQL_PORT}"
if ! compose up --detach sqlserver; then
    printf '\nSQL Server could not start. If Docker is using port %s, either stop it with:\n' "${PODMAN_SQL_PORT}" >&2
    printf '  docker compose stop sqlserver\n' >&2
    printf 'or set PODMAN_SQL_PORT=1434 in .env and retry.\n' >&2
    exit 1
fi

container_id="$(sqlserver_container_id)"
[[ -n "${container_id}" ]] || fail "Compose did not create the SQL Server container."

printf 'Waiting for SQL Server health check'
for _ in {1..60}; do
    state="$(podman inspect --format '{{if .State.Health}}{{.State.Health.Status}}{{else}}{{.State.Status}}{{end}}' "${container_id}")"
    if [[ "${state}" == "healthy" ]]; then
        printf '\nSQL Server is healthy on localhost:%s.\n' "${PODMAN_SQL_PORT}"
        exit 0
    fi
    if [[ "${state}" == "exited" || "${state}" == "unhealthy" ]]; then
        printf '\n'
        compose logs --tail 200 sqlserver
        fail "SQL Server entered state '${state}'."
    fi
    printf '.'
    sleep 2
done

printf '\n'
compose logs --tail 200 sqlserver
fail "SQL Server did not become healthy within 120 seconds."
