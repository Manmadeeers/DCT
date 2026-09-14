namespace Warehouse.Models.DTOs
{
    public class ProductDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public int? Stock_id { get; set; }
    }

    public class CreateProductRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public int? Stock_id { get; set; }
    }

    public class UpdateProductRequest
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public decimal? Price { get; set; }
        public int? Stock_Id { get; set; }
    }
}