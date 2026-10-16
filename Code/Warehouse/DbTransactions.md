0. Run database container

```bash
docker compose up -d
```

1. Run server

go to Code/Warehouse/src and run:
```bash
dotnet run --project Warehouse.csproj -- --urls http://localhost:5000 --Lab:RequestDelaySeconds=0 --Lab:RaceDelaySeconds=10 --Lab:TransactionFailStep=1
```

2. Run the first client

Create a new terminal, go to /Code and run:
```bash
dotnet run  --project ./Client/Client.csproj -- 1 http://localhost:5000 0
```

3. Run the second client

Split the previously created terminal(directory must be /Code) and run:
```bash
dotnet run  --project ./Client/Client.csproj -- 2 http://localhost:5000 0
```

4.Check the warehouse and stock id we're working with

Go to a terminal where client1 is running and prompt this:
```text
GET /api/bootstrap
```
Response should be something like:
```json
[18:47:47.929] Client 1 <- HTTP 200
{"warehouseId":1,"stockId":1,"supplierId":1}
```

5. Create a new product to work with

From a terminal where client1 is running prompt this:
```text
POST /api/operations/AddProduct
```

After this add the folowing body to a request:

```json
{
  "sku": "TRAN-DEMO-03",
  "name": "Transaction demo product"
}
```

Response should be something like:
```json
[18:51:31.473] Client 1 -> SEND
[18:51:31.608] Client 1 <- HTTP 200
{"id":13,"sku":"SYNC-DEMO-01","name":"Race demo product"}
> 
```

6.Use a fail shipment method to demonstrate the database state when transactions do not handle failures

From a terminal where client1 is running prompt this:

```text
POST /api/operations/ReceiveShipmentFail
```

And add the folowing body to a request(Make sure that warehouse, stock and product ids are valid):
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

Response should be something like:
```json
[08:55:16.814] Client 1 -> SEND
[08:55:16.959] Client 1 <- HTTP 500
{"title":"Internal server error","status":500,"detail":"An unexpected server error occurred.","instance":"/api/operations/ReceiveShipmentFail","traceId":"0HNPB92N217G2:00000001"}
```

And on the server side you should see something like:
```json
fail: Warehouse.Api.Infrastructure.GlobalExceptionHandler[0]
      Request failed: 500
      System.InvalidOperationException: Artificial failure after saving the shipment.
```
After that go to a database client and prompt this:

```sql
select * from shipmentItem where shipmentid=23;
select * from shipment where id=23;
```

You should see no ShipmentItems for a shipment but the shipment should be present

7. Use a transaction ReseiveShipment method(with fail)

From the terminal where client1 is running prompt this:

```text
POST /api/operaions/ReceiveShipment
```

Add the folowing body to a request:

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

This should fail giving 500 error code.
Then check the database. Empty transaction will not be created because ROLLBACK kicked in

8. Restart the server

Restart the server setting the TransactionFailStep option to 0 disabling artificial error in ReceiveShipment transaction methods

```bash
dotnet run --project Warehouse.csproj -- --urls http://localhost:5000 --Lab:RequestDelaySeconds=0 --Lab:RaceDelaySeconds=10 --Lab:TransactionFailStep=0
```

9. Repeat ReceiveShipment call

```text
POST /api/operaions/ReceiveShipment
```
Add the folowing body to a request:

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

This should finish successfully