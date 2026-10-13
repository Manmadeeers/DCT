0. Run database container
```bash
docker compose up -d
```

1. Run server
```bash
dotnet run --project src/Warehouse.csproj -- --urls http://localhost:5000 --Lab:RequestDelaySeconds=0 --Lab:RaceDelaySeconds=10
```

2. Run client1

```bash
dotnet run --no-build --project ../Client/Client.csproj -- 1 http://localhost:5000
```

3. Run client2

```bash
dotnet run --no-build --project ../Client/Client.csproj -- 2 http://localhost:5000
```


4. Chekc warehouse state

```text
GET /api/bootstrap
```

5. From client1 create a new product. Change SKU if needed

```text
POST /api/operations/AddProduct
```

```json
{
  "sku": "RACE-DEMO-05",
  "name": "Race demo product"
}
```


6.ship 1 previously created product from Client1
```text
POST /api/operations/ReceiveShipment
```

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

7. Check warehouse from client1

```text
GET /api/stocks/1
```

Should:  Quantity=1, Reserved=0, Available=1.

8. Reserve 1 from Client1;

```text
POST /api/operations/RaceReserve
```

```json
{
  "warehouseId": 1,
  "stockId": 1,
  "productId": 20,
  "quantity": 1
}
```

9. After prompting a request to Client1 do on Client2:

```text
POST /api/operations/RaceReserve
```

```json
{
  "warehouseId": 1,
  "stockId": 1,
  "productId": 12,
  "quantity": 1
}
```

10. Wait. 200-ok. 400- we're later

11. Read final warehouse state

```text
GET /api/stocks/1
```
