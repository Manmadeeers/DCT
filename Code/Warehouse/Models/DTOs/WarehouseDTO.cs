namespace Warehouse.Models.DTOs;

public class WarehouseDTO
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
}

public class CreateWarehouseRequest
{
    public string Name { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
}

public class UpdateWarehouseRequest
{
    public string? Name { get; set; }
    public string? Country { get; set; }
    public string? City { get; set; }
}