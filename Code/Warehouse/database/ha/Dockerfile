FROM postgres:18.6

RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        python3 \
        python3-venv \
        ca-certificates \
    && python3 -m venv /opt/patroni \
    && /opt/patroni/bin/pip install --no-cache-dir \
        "patroni[psycopg3,etcd3]==4.1.5" \
    && rm -rf /var/lib/apt/lists/*

ENV PATH="/opt/patroni/bin:${PATH}"

COPY --chmod=755 patroni-entrypoint.sh /usr/local/bin/patroni-entrypoint.sh

ENTRYPOINT ["/usr/local/bin/patroni-entrypoint.sh"]