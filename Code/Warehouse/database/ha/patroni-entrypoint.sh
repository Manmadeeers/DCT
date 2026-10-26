#!/usr/bin/env bash
set -euo pipefail

chown postgres:postgres /var/lib/postgresql

install -d \
    -o postgres \
    -g postgres \
    -m 0700 \
    /var/lib/postgresql/18

install -d \
    -o postgres \
    -g postgres \
    -m 3775 \
    /var/run/postgresql

exec gosu postgres \
    /opt/patroni/bin/patroni \
    /etc/patroni.yml