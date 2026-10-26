#!/usr/bin/env bash
set -euo pipefail

if [ ! -s "$PGDATA/PG_VERSION" ]; then
    echo "No existing PostgreSQL database found at $PGDATA." >&2
    echo "Refusing to initialize an empty database." >&2
    exit 1
fi

exec /usr/local/bin/docker-entrypoint.sh "$@"