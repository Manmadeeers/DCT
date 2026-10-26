# Database transaction instructions(lab4)

## 0. Run database container

```bash
docker  --context desktop-linux -f docker-compose.yaml compose up -d
```

## 1. Run the server

```bash
dotnet run --project src/Warehouse.csproj -- --urls http://localhost:5000 --Lab:RequestDelaySeconds=0 --Lab:RaceDelaySeconds=10 --TransactionFailStep=1
```

## 2. Run client1

#### In a separate (preferably splitted) terminal run:

```bash
dotnet run --no-build --project ../Client/Client.csproj -- 1 http://localhost:5000
```

## 3. Run client2

#### In a separate terminal run:

```bash
dotnet run --no-build --project ../Client/Client.csproj -- 2 http://localhost:5000
```


## 4. Chech warehouse state

#### From one of the client's terminals run: 

```text
GET /api/bootstrap
```

#### Expect something like:

```text
[10:22:07.852] Client 1 -> SEND
[10:22:07.954] Client 1 <- HTTP 200
{"warehouseId":1,"stockId":1,"supplierId":1}
```

## 5. Create a new product to work with

#### From one of the clients enter this request path:

```text
POST /api/operations/AddProduct
```

#### Enter this request body:

```json
{
  "sku": "TRAN-DEMO-03",
  "name": "Transaction demo product"
}
```

#### Expect response like this:
```json
[18:51:31.473] Client 1 -> SEND
[18:51:31.608] Client 1 <- HTTP 200
{"id":13,"sku":"SYNC-DEMO-01","name":"Race demo product"}
```

## 6.Use a fail shipment method to demonstrate the database state when transactions do not handle failures

#### From client1's terminal puth this request path:

```text
POST /api/operations/ReceiveShipmentFail
```

#### Enterh this request body(check productId field):

```json
{
  "warehouseId": 1,
  "stockId": 1,
  "supplierId": 1,
  "items": [
    {
      "productId": 28,
      "quantity": 1
    }
  ]
}
```

#### Response should be something like:
```json
[08:55:16.814] Client 1 -> SEND
[08:55:16.959] Client 1 <- HTTP 500
{"title":"Internal server error","status":500,"detail":"An unexpected server error occurred.","instance":"/api/operations/ReceiveShipmentFail","traceId":"0HNPB92N217G2:00000001"}
```

#### And on the server side you should see something like:

```text
fail: Warehouse.Api.Infrastructure.GlobalExceptionHandler[0]
      Request failed: 500
      System.InvalidOperationException: Artificial failure after saving the shipment.
```

#### After that go to a database client and prompt this(check shipmentId and id fields):

```sql
select * from shipmentItem where shipmentid=23;
select * from shipment where id=23;
```

#### You should see no ShipmentItems for a shipment but the shipment should be present

## 7. Use a transaction ReseiveShipment method(with fail)

#### From the terminal where client1 is running prompt this:

```text
POST /api/operaions/ReceiveShipment
```

#### Add the folowing body to a request(check productId field):

```json
{
  "warehouseId": 1,
  "stockId": 1,
  "supplierId": 1,
  "items": [
    {
      "productId": 25,
      "quantity": 4
    }
  ]
}
```

#### This should fail giving 500 error code. Then check the database. Empty transaction will not be created because ROLLBACK kicked in

## 8. Restart the server

#### Restart the server setting the TransactionFailStep option to 0 disabling artificial error in ReceiveShipment transaction methods

```bash
dotnet run --project Warehouse.csproj -- --urls http://localhost:5000 --Lab:RequestDelaySeconds=0 --Lab:RaceDelaySeconds=10 --Lab:TransactionFailStep=0
```

## 9. Repeat ReceiveShipment request

#### from a terminal where a client is running put this request path:

```text
POST /api/operaions/ReceiveShipment
```
#### Add the folowing body to a request:(check productId field):

```json
{
  "warehouseId": 1,
  "stockId": 1,
  "supplierId": 1,
  "items": [
    {
      "productId": 25,
      "quantity": 4
    }
  ]
}
```

#### This should finish successfully