using Warehouse.Models.DTOs;
using Warehouse.Models.Entities;

namespace Warehouse.Services.Interfaces;

public interface IWarehouseService
{
    //Product
    Task<ProductDTO> AddProductAsync(CreateProductRequest request, CancellationToken ct = default);
}