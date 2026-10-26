# Race condition instruction(lab2)

## 0. Run database container

```bash
docker --context desktop-linux compose up -d
```
##### (If you have just one docker engine do not use context)

## 1. Run the server

```bash
dotnet run --project src/Warehouse.csproj -- --urls http://localhost:5000 --Lab:RequestDelaySeconds=0 --Lab:RaceDelaySeconds=10
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

## 5. From client1 create a new product. Change SKU if needed


#### In a terminal put this request path:

```text
POST /api/operations/AddProduct
```

#### Enter this json body:

```json
{
  "sku": "RACE-DEMO-05",
  "name": "Race demo product"
}
```

## 6.Ship 1 previously created product

#### From client1's terminal puth this request path: 

```text
POST /api/operations/ReceiveShipment
```

#### Enter this json body(check the productID field. It should match with an id of previously created product)

```json
{
  "warehouseId": 1,
  "stockId": 1,
  "supplierId": 1,
  "items": [
    {
      "productId": 17,
      "quantity": 1
    }
  ]
}
```

## 7. Check warehouse from client1

#### From client1's terminal put this request path:

```text
GET /api/stocks/1
```

#### Search for your shipped product. It's data should be something like:

```text
Quantity=1, Reserved=0, Available=1.
```

## 8. Reserve a product from Client1(DO NOT SEND A REQUEST YET)

#### From client1's terminal put this request path:

```text
POST /api/operations/RaceReserve
```

#### Enter this json body(Check productId field):

```json
{
  "warehouseId": 1,
  "stockId": 1,
  "productId": 20,
  "quantity": 1
}
```

## 9. Reserve a product from Client2(DO NOT SEND A REQUEST YET)

#### From client2's terminal puth this request path:

```text
POST /api/operations/RaceReserve
```

#### Enter this json body(check productId field):

```json
{
  "warehouseId": 1,
  "stockId": 1,
  "productId": 12,
  "quantity": 1
}
```

## 10. Send a request from client1 and immidiately after that - from client 2

#### You're gonna have aprox. 10 seconds. After that responses will be received

## 11. Check responses

#### If everything was done the right way you should get 200-OK on both clients. If on one of them you get 400-BadRequest you missed the timing and shoul drun the whole chain again
