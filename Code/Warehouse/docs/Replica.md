# Database replication instruction

## 1. Start your services

#### In a terminal run(do not use --context if not needed):

```bash
docker --context desktop-linux compose -f docker-compose.yaml up -d
```

## 2. Start the server

#### In a new terminal run:

```bash
dotnet run --project src/Warehouse.csproj
```

## 3. Start the recovery watcher

#### In a new terminal run:

```bash
python3 -u database/ha/recover_containers.py
```

## 4. Check the database cluster

#### From a new terminal run:

```bash
docker --context desktop-linux exec dct-replica patronictl -c /etc/patroni.yml list
```

#### Response should be something like:

```text
+ Cluster: warehouse (7700280141267095592) -----+-----------+----+-------------+-----+------------+-----+
| Member      | Host             | Role         | State     | TL | Receive LSN | Lag | Replay LSN | Lag |
+-------------+------------------+--------------+-----------+----+-------------+-----+------------+-----+
| dct         | postgres         | Leader       | running   |  3 |             |     |            |     |
| dct-replica | postgres-replica | Sync Standby | streaming |  3 |   0/3008220 |   0 |  0/3008220 |   0 |
+-------------+------------------+--------------+-----------+----+-------------+-----+------------+-----+
```

## 5. Run a console client and add a product(to mark a state before recovery)

#### From a new terminal run:

```bash
dotnet run --project ./Client/Client.csproj -- 1 http://localhost:5000 0
```

#### In an appeared client terminal enter this request path:

```text
POST /api/operations/AddProduct
```

#### After that enter the folowing json request body:

```json
{
  "sku":"BEFORE-FAILOVER-02",
  "name":"Before failover product"
}
```

## 6. Prepare a client to add a product after a failover

#### In a client terminal enter this request path:

```text
POST /api/operations/AddProduct
```

#### Enter the folowing json request body(DO NOT SEND A REQUEST YET):

```json
{
  "sku":"AFTER-FAILOVER-04",
  "name":"After failover product"
}
```

## 7. Simulate a failover

#### From a free terminal run(you should kill whatever instance marked as Leader in a previous step)

```bash
docker --context desktop-linux kill dct
```

#### If you go to the terminal wehre a revocery watcher is running, it should print a message like this:

```text
dct is down. dct-replica is primary. Recovery will start in 60 seconds.
```

## 8. Send a prepared request from a clint

#### Send a request and wait for Patroni to perform a recovery. You can watch detailed steps in a recovery watcher terminal

## 9. Check products added before and after a failover

#### You can use an endpoint:

```text
GET /api/products
```

#### Or you can open a database client and run the folowing:

```sql
select * from product;
```
