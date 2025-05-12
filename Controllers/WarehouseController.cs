using Microsoft.AspNetCore.Mvc;
using Tutorial9.Model;
using Tutorial9.Services;

namespace Tutorial9.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WarehouseController : ControllerBase
{
    private readonly DataAcces _dataAccess;

    public WarehouseController(DataAcces dataAccess)
    {
        _dataAccess = dataAccess;
    }

    [HttpPost]
    public async Task<IActionResult> AddProduct([FromBody] ProductWarehouse request)
    {
        if (request.Amount <= 0)
            return BadRequest("Amount must be greater than 0");

        var result = await _dataAccess.AddProductToWarehouse(request);

        if (result == null)
            return BadRequest("Validation failed or operation could not be completed.");

        return Ok(new ProductWarehouseResponse { IdProductWarehouse = result.Value });
    }
}