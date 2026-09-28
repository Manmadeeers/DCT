
INSERT INTO warehouse (name) VALUES ('Central Warehouse') ON CONFLICT (name) DO NOTHING;
INSERT INTO suplier (name) VALUES ('Main Supplier') ON CONFLICT (name) DO NOTHING;
INSERT INTO stock (warehouseid, name)
SELECT w.id, 'Main Sorting Zone'
FROM warehouse AS w
WHERE w.name = 'Central Warehouse'
  AND NOT EXISTS (
      SELECT 1 FROM stock AS s
      WHERE s.warehouseid = w.id AND s.name = 'Main Sorting Zone'
  );
