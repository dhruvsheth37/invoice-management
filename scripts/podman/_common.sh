#!/usr/bin/env bash

set -Eeuo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
REPOSITORY_ROOT="$(cd -- "${SCRIPT_DIR}/../.." && pwd)"
COMPOSE_FILE="${REPOSITORY_ROOT}/podman-compose.yml"
ENV_FILE="${REPOSITORY_ROOT}/.env"

fail() {
    printf 'Error: %s\n' "$1" >&2
    exit 1
}

require_command() {
    command -v "$1" >/dev/null 2>&1 || fail "Required command '$1' was not found."
}

load_environment() {
    [[ -f "${ENV_FILE}" ]] || fail "${ENV_FILE} is missing. Copy .env.example to .env and set SQL_SA_PASSWORD."

    set -a
    # shellcheck disable=SC1090
    source "${ENV_FILE}"
    set +a

    [[ -n "${SQL_SA_PASSWORD:-}" ]] || fail "SQL_SA_PASSWORD is missing from ${ENV_FILE}."
    export PODMAN_SQL_PORT="${PODMAN_SQL_PORT:-1433}"
}

ensure_podman_available() {
    require_command podman

    if ! podman info >/dev/null 2>&1; then
        if [[ "$(uname -s)" == "Darwin" ]]; then
            printf 'Starting the Podman machine...\n'
            podman machine start >/dev/null 2>&1 ||
                fail "Podman machine is unavailable. Run 'podman machine init' once, then retry."
        fi
    fi

    podman info >/dev/null 2>&1 || fail "Cannot connect to the Podman service."
}

compose() {
    podman compose --file "${COMPOSE_FILE}" "$@"
}

sqlserver_container_id() {
    compose ps --quiet sqlserver 2>/dev/null || true
}

require_sqlserver_running() {
    local container_id
    container_id="$(sqlserver_container_id)"
    [[ -n "${container_id}" ]] || fail "The Podman SQL Server service is not running. Run scripts/podman/start.sh first."

    [[ "$(podman inspect --format '{{.State.Running}}' "${container_id}")" == "true" ]] ||
        fail "The Podman SQL Server container is stopped. Run scripts/podman/start.sh first."
}
