#!/usr/bin/env bash
set -euo pipefail

: "${PGDATA:?PGDATA is required}"
: "${UPSTREAM_HOST:?UPSTREAM_HOST is required}"
: "${NODE_NAME:?NODE_NAME is required}"
: "${SLOT_NAME:?SLOT_NAME is required}"
: "${REPLICATION_PASSWORD:?REPLICATION_PASSWORD is required}"

if [ -d "$PGDATA" ] && [ -n "$(ls -A "$PGDATA")" ]; then
    if [ "${RESEED:-no}" != "yes" ]; then
        echo "The target data directory is not empty." >&2
        echo "To rebuild it, stop the target database first." >&2
        echo "Then run this tool with RESEED=yes." >&2
        exit 1
    fi

    previous_directory="${PGDATA}.before-reseed.$(date -u +%Y%m%dT%H%M%SZ)"

    if [ -e "$previous_directory" ]; then
        echo "Backup directory already exists: $previous_directory" >&2
        exit 1
    fi

    mv "$PGDATA" "$previous_directory"

    echo "Previous database files retained at: $previous_directory"
fi

install -d -m 0700 -o postgres -g postgres "$PGDATA"

export PGOPTIONS="-c synchronous_commit=off"

gosu postgres pg_basebackup \
    --dbname="host=$UPSTREAM_HOST port=5432 user=warehouse_replicator password=$REPLICATION_PASSWORD application_name=$NODE_NAME connect_timeout=5" \
    --pgdata="$PGDATA" \
    --wal-method=stream \
    --checkpoint=fast \
    --write-recovery-conf \
    --create-slot \
    --slot="$SLOT_NAME" \
    --progress

printf "\nsynchronous_standby_names = 'FIRST 1 (*)'\n" \
    >> "$PGDATA/postgresql.auto.conf"

chown postgres:postgres "$PGDATA/postgresql.auto.conf"

echo "Database clone completed."