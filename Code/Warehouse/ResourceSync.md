0. Run database container

```bash
docker compose up -d
```

1. Run server

go to Code/Warehouse/src and run:
```bash
dotnet run --project Warehouse.csproj -- --urls http://localhost:5000 --Lab:RequestDelaySeconds=0 --Lab:RaceDelaySeconds=10
```

2. Run the first client

Create a new terminal, go to /Code and run:
```bash
dotnet run --no-build --project ./Client/Client.csproj -- 1 http://localhost:5000
```

3. Run the second client

Split the previously created terminal(directory must be /Code) and run:
```bash
dotnet run --no-build --project ./Client/Client.csproj -- 2 http://localhost:5000
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
  "sku": "SYNC-DEMO-08",
  "name": "Race demo product"
}
```

Response should be something like:
```json
[18:51:31.473] Client 1 -> SEND
[18:51:31.608] Client 1 <- HTTP 200
{"id":13,"sku":"SYNC-DEMO-01","name":"Race demo product"}
> 
```

6.Ship previously created product to a warehouse

From a terminal where client1 is running prompt this:

```text
POST /api/operations/ReceiveShipment
```

And add the folowing body to a request(Make sure that warehouse, stock and product ids are valid):
```json
{
  "warehouseId": 1,
  "stockId": 1,
  "supplierId": 1,
  "items": [
    {
      "productId": 22,
      "quantity": 1
    }
  ]
}
```

Response should be something like:
```json
[18:53:34.132] Client 1 -> SEND
[18:53:34.337] Client 1 <- HTTP 200
{"id":12,"warehouseId":1,"stockId":1,"supplierId":1,"items":[{"productId":13,"quantity":1}]}
> 
```

7. Check the stock where a product was shipped:

From the terminal where client1 is running prompt this(make sure that the stock id is valid):

```text
GET /api/stocks/1
```

Response should be something like:
```json
[18:54:52.561] Client 1 <- HTTP 200
{"id":1,"warehouseId":1,"name":"Main Sorting Zone","items":[{"productId":1,"quantity":80,"reserved":0,"available":80},{"productId":3,"quantity":40,"reserved":3,"available":37},{"productId":4,"quantity":100,"reserved":0,"available":100},{"productId":5,"quantity":90,"reserved":0,"available":90},{"productId":10,"quantity":1,"reserved":1,"available":0},{"productId":11,"quantity":1,"reserved":1,"available":0},{"productId":12,"quantity":1,"reserved":1,"available":0},{"productId":13,"quantity":1,"reserved":0,"available":1}]}
>
```
Make sure that the previously created product was correctly shipped to a warehouse.

8. Reserve a product from client1 side:

From a terminal where client1 is running prompt:

```text
POST /api/operations/SyncReserve
```

Add the folowing body to a request(make sure that the product id is valid):
```json
{
  "warehouseId": 1,
  "stockId": 1,
  "productId": 22,
  "quantity": 1
}
```

9. Attempt to reserve the same product from client2 side:

From a terminal where client2 is running prompt:
```text
POST /api/operations/SyncReserve
```

Add the folowing body to a request(make sure that the product id is valid):
```json
{
  "warehouseId": 1,
  "stockId": 1,
  "productId": 19,
  "quantity": 1
}
```

From the moment the JSON body was sent on the client1 side there's 10 seconds to do the same on the client2 side.

10. Wait for some time
As a result on the client1 side there should be something like:
```json
{  "warehouseId": 1,  "stockId": 1,  "productId": 15,  "quantity": 1}
[19:14:06.178] Client 1 -> SEND
[19:14:16.264] Client 1 <- HTTP 200
{"id":1,"warehouseId":1,"name":"Main Sorting Zone","items":[{"productId":1,"quantity":80,"reserved":0,"available":80},{"productId":3,"quantity":40,"reserved":3,"available":37},{"productId":4,"quantity":100,"reserved":0,"available":100},{"productId":5,"quantity":90,"reserved":0,"available":90},{"productId":10,"quantity":1,"reserved":1,"available":0},{"productId":11,"quantity":1,"reserved":1,"available":0},{"productId":12,"quantity":1,"reserved":1,"available":0},{"productId":13,"quantity":1,"reserved":1,"available":0},{"productId":14,"quantity":1,"reserved":1,"available":0},{"productId":15,"quantity":1,"reserved":1,"available":0}]}
> 
```

And on the client2 side there should be something like:
```json
{  "warehouseId": 1,  "stockId": 1,  "productId": 15,  "quantity": 1}
[19:14:07.720] Client 2 -> SEND
[19:14:16.278] Client 2 <- HTTP 400
{"title":"Incorrect input data","status":400,"detail":"Requested 1; available 0.","instance":"/api/operations/SyncReserve","traceId":"0HNP8FBL6MCMT:00000001"}
> 
```

11. Check the reserved product:

On any client run the folowing:

```text
GET /api/stocks/1
```

As a response there should be something like:
```json
"reserved":0,"available":80},{"productId":3,"quantity":40,"reserved":3,"available":37},{"productId":4,"quantity":100,"reserved":0,"available":100},{"productId":5,"quantity":90,"reserved":0,"available":90},{"productId":10,"quantity":1,"reserved":1,"available":0},{"productId":11,"quantity":1,"reserved":1,"available":0},{"productId":12,"quantity":1,"reserved":1,"available":0},{"productId":13,"quantity":1,"reserved":1,"available":0},{"productId":14,"quantity":1,"reserved":1,"available":0},{"productId":15,"quantity":1,"reserved":1,"available":0}]}
> 
```

12. Repeat the same sequence for POST /api/operations/SemaphoreReserve to demonstrate shared resource sync via SemaphoreSlim.
