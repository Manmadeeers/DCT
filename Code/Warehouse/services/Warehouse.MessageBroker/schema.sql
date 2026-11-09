CREATE SCHEMA IF NOT EXISTS warehouse_broker;

CREATE TABLE IF NOT EXISTS warehouse_broker.queues
(
    name text PRIMARY KEY
);

CREATE TABLE IF NOT EXISTS warehouse_broker.bindings
(
    queue_name text NOT NULL
        REFERENCES warehouse_broker.queues(name),

    pattern text NOT NULL,

    PRIMARY KEY (queue_name, pattern)
);

CREATE TABLE IF NOT EXISTS warehouse_broker.messages
(
    event_id uuid PRIMARY KEY,
    routing_key text NOT NULL,
    envelope jsonb NOT NULL,
    created_at timestamptz NOT NULL DEFAULT clock_timestamp()
);

CREATE TABLE IF NOT EXISTS warehouse_broker.deliveries
(
    queue_name text NOT NULL
        REFERENCES warehouse_broker.queues(name),

    event_id uuid NOT NULL
        REFERENCES warehouse_broker.messages(event_id),

    position bigint GENERATED ALWAYS AS IDENTITY,

    state text NOT NULL DEFAULT 'ready'
        CHECK (state IN ('ready', 'inflight', 'done', 'dead')),

    attempts integer NOT NULL DEFAULT 0,

    available_at timestamptz NOT NULL DEFAULT clock_timestamp(),

    lease_token uuid,
    lease_until timestamptz,
    completed_at timestamptz,
    last_error text,

    PRIMARY KEY (queue_name, event_id)
);

CREATE INDEX IF NOT EXISTS ix_broker_ready
    ON warehouse_broker.deliveries
    (queue_name, available_at, position)
    WHERE state = 'ready';

CREATE INDEX IF NOT EXISTS ix_broker_expired
    ON warehouse_broker.deliveries (lease_until)
    WHERE state = 'inflight';

CREATE UNIQUE INDEX IF NOT EXISTS ix_broker_receipt
    ON warehouse_broker.deliveries (lease_token)
    WHERE lease_token IS NOT NULL;

INSERT INTO warehouse_broker.queues(name)
VALUES
    ('warehouse.audit'),
    ('warehouse.notifications'),
    ('warehouse.catalog'),
    ('warehouse.analytics')
ON CONFLICT DO NOTHING;

INSERT INTO warehouse_broker.bindings(queue_name, pattern)
VALUES
    ('warehouse.audit', 'warehouse.#'),
    ('warehouse.notifications', 'warehouse.stock.updated'),
    ('warehouse.catalog', 'warehouse.product.added'),
    ('warehouse.analytics', 'warehouse.#')
ON CONFLICT DO NOTHING;