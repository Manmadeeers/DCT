WITH warehouse_rows AS (
    SELECT
        'warehouse'::text AS table_name,
        to_jsonb(t) AS row_data
    FROM public.warehouse AS t

    UNION ALL

    SELECT
        'stock',
        to_jsonb(t)
    FROM public.stock AS t

    UNION ALL

    SELECT
        'product',
        to_jsonb(t)
    FROM public.product AS t

    UNION ALL

    SELECT
        'suplier',
        to_jsonb(t)
    FROM public.suplier AS t

    UNION ALL

    SELECT
        'stockitem',
        to_jsonb(t)
    FROM public.stockitem AS t

    UNION ALL

    SELECT
        'shipment',
        to_jsonb(t)
    FROM public.shipment AS t

    UNION ALL

    SELECT
        'shipmentitem',
        to_jsonb(t)
    FROM public.shipmentitem AS t
)
SELECT jsonb_build_object(
    'table', table_name,
    'row', row_data
)::text
FROM warehouse_rows
ORDER BY
    table_name COLLATE "C",
    row_data::text COLLATE "C";