# Custom publisher/subscriber setup

The default Warehouse messaging path uses two instances of the in-project HTTP
broker backed by the same Patroni PostgreSQL cluster. Clients rotate between
the brokers and fail over to the other endpoint on connection failures,
timeouts, or HTTP 5xx responses. RabbitMQ source files and the
RabbitMQ-specific compose setup remain available, but `docker-compose.yaml`
does not start RabbitMQ.

## Start the broker and subscribers

Run this command from `Code/Warehouse`:

```bash
docker --context desktop-linux compose -f docker-compose.yaml up -d --build
```

The compose build publishes the broker and subscriber projects directly from
their current source. A separate `dotnet publish` step is not required.

Check the custom broker and its queues:

```bash
curl http://localhost:5080/health
curl http://localhost:5081/health
curl http://localhost:5080/api/queues
```

The configured durable queues are:

- `warehouse.audit` for `warehouse.#`
- `warehouse.notifications` for `warehouse.stock.updated`
- `warehouse.catalog` for `warehouse.product.added`
- `warehouse.analytics` for `warehouse.#`

## Start the Warehouse API

From `Code/Warehouse`, run:

```bash
dotnet run --project src/Warehouse.csproj
```

The API rotates between `http://localhost:5080/` and
`http://localhost:5081/` using the `Broker:Endpoints` section in
`src/appsettings.json`.

To verify broker failover, stop either broker and continue publishing:

```bash
docker --context desktop-linux compose -f docker-compose.yaml \
  stop message-broker

docker --context desktop-linux compose -f docker-compose.yaml \
  start message-broker
```

## Exercise message delivery

Use the client or Swagger to call operations such as:

```text
POST /api/operations/AddProduct
POST /api/operations/ReceiveShipment
POST /api/operations/ReserveProduct
```

Example request bodies:

```json
{
  "sku": "PubSub-test-06",
  "name": "PubSub test product"
}
```

```json
{
  "warehouseId": 1,
  "stockId": 1,
  "supplierId": 1,
  "items": [
    {
      "productId": 238,
      "quantity": 1
    }
  ]
}
```

```json
{
  "warehouseId": 1,
  "stockId": 1,
  "productId": 238,
  "quantity": 1
}
```

Follow subscriber output with:

```bash
docker --context desktop-linux compose -f docker-compose.yaml logs -f \
  audit-subscriber notification-subscriber catalog-subscriber \
  eventanalytics-subscriber
```

## Verify durable delivery

Stop a subscriber, publish matching events, and start it again:

```bash
docker --context desktop-linux compose -f docker-compose.yaml \
  stop notification-subscriber

docker --context desktop-linux compose -f docker-compose.yaml \
  start notification-subscriber
```

Messages remain in PostgreSQL until that subscriber acknowledges them. Failed
deliveries are retried three times and can then be inspected at:

```text
GET http://localhost:5080/api/queues/{queue}/deadletters
```

## RabbitMQ implementation

The RabbitMQ implementation remains in:

- `messaging/Warehouse.Messaging/RabbitOptions.cs`
- `messaging/Warehouse.Messaging/RabbitTopology.cs`
- `messaging/Warehouse.Messaging/RabbitSubscriber.cs`
- `src/Infrastructure/RabbitPublisherWorker.cs`
- `messaging/rabbitmq/rabbitmq.conf`
- `compose.pubsub.yaml`

It is retained as an alternative implementation and is not part of the default
custom-broker run described above.
