# Synchronization instruction(lab3)

## 0. Run database container

#### Run this in the terminal:

```bash
docker --context desktop-linux  -f docker-compose.yaml compose up -d
```

## 1. Run the server

#### From the separate terminal run: 

```bash
dotnet run --project src/Warehouse.csproj -- --urls http://localhost:5000 --Lab:RequestDelaySeconds=0 --Lab:RaceDelaySeconds=10
```

## 2. Run client1

#### From the separate(preferably splitted) terminal run: 

```bash
dotnet run  --project ./Client/Client.csproj -- 1 http://localhost:5000
```

## 3. Run client2

#### From the separate terminal run:

```bash
dotnet run  --project ./Client/Client.csproj -- 2 http://localhost:5000
```

## 4.Check the warehouse state

#### From one of the client put this request path:

```text
GET /api/bootstrap
```

#### Expect something like: 

```json
[18:47:47.929] Client 1 <- HTTP 200
{"warehouseId":1,"stockId":1,"supplierId":1}
```

## 5. Create a new product to work with

#### From client1's terminal put this request path:

```text
POST /api/operations/AddProduct
```
#### Enter this request body:

```json
{
  "sku": "SYNC-DEMO-09",
  "name": "Race demo product"
}
```

#### Expect response like this:

```json
[18:51:31.473] Client 1 -> SEND
[18:51:31.608] Client 1 <- HTTP 200
{"id":13,"sku":"SYNC-DEMO-01","name":"Race demo product"}
```

## 6.Ship previously created product to a warehouse

#### From client1's terminal puth this request path:

```text
POST /api/operations/ReceiveShipment
```

#### Enter this request body(check productId field):

```json
{
  "warehouseId": 1,
  "stockId": 1,
  "supplierId": 1,
  "items": [
    {
      "productId": 24,
      "quantity": 1
    }
  ]
}
```

#### Expect response like:

```json
[18:53:34.132] Client 1 -> SEND
[18:53:34.337] Client 1 <- HTTP 200
{"id":12,"warehouseId":1,"stockId":1,"supplierId":1,"items":[{"productId":13,"quantity":1}]}
```


## 7. Reserve a product from client1's side(DO NOT SEND A REQUEST YET)

#### Put this request path:

```text
POST /api/operations/SyncReserve
```

#### Enter this request body(check productId field):

```json
{
  "warehouseId": 1,
  "stockId": 1,
  "productId": 24,
  "quantity": 1
}
```

## 8. Reserve a product from client2's side(DO NOT SEND A REQUEST YET)

#### Put this request path:

```text
POST /api/operations/SyncReserve
```

#### Enter this request body(check productId field):

```json
{
  "warehouseId": 1,
  "stockId": 1,
  "productId": 28,
  "quantity": 1
}
```

## 9. Send a request from client1 and then immidiately from client2

## 10. Wait for some time and check reponses

#### Client1's reponse should be like this:

```json
{  "warehouseId": 1,  "stockId": 1,  "productId": 15,  "quantity": 1}
[19:14:06.178] Client 1 -> SEND
[19:14:16.264] Client 1 <- HTTP 200
{"id":1,"warehouseId":1,"name":"Main Sorting Zone","items":[{"productId":1,"quantity":80,"reserved":0,"available":80},{"productId":3,"quantity":40,"reserved":3,"available":37},{"productId":4,"quantity":100,"reserved":0,"available":100},{"productId":5,"quantity":90,"reserved":0,"available":90},{"productId":10,"quantity":1,"reserved":1,"available":0},{"productId":11,"quantity":1,"reserved":1,"available":0},{"productId":12,"quantity":1,"reserved":1,"available":0},{"productId":13,"quantity":1,"reserved":1,"available":0},{"productId":14,"quantity":1,"reserved":1,"available":0},{"productId":15,"quantity":1,"reserved":1,"available":0}]}
```

#### Client2 response should be like this:

```json
{  "warehouseId": 1,  "stockId": 1,  "productId": 15,  "quantity": 1}
[19:14:07.720] Client 2 -> SEND
[19:14:16.278] Client 2 <- HTTP 400
{"title":"Incorrect input data","status":400,"detail":"Requested 1; available 0.","instance":"/api/operations/SyncReserve","traceId":"0HNP8FBL6MCMT:00000001"}
```


## 11. Repeat the same sequence for POST /api/operations/SemaphoreReserve

#### Repeate the same sequence from step 5 for the mentioned URL
