namespace Warehouse.Models.DTOs;

public class WarehouseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public DateTime Created_at { get; set; }
    public DateTime? Updated_at { get; set; }
}

public class CreateWarehouseRequest
{
    public string Name { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int Capacity { get; set; }
}

public class UpdateWarehouseRequest
{
    public string? Name { get; set; }
    public string? Country { get; set; }
    public string? City { get; set; }
    public string? Email { get; set; }
    public int? Capacity { get; set; }
}