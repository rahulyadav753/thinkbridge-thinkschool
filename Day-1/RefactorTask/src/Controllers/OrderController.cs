using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RefactorTask.Dtos;
using RefactorTask.Services;

namespace RefactorTask.Controllers;

[ApiController]
[Route("api/orders")]
public class OrderController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly ILogger<OrderController> _logger;

    public OrderController(IOrderService orderService, ILogger<OrderController> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<OrderResponse>> CreateOrder([FromBody] OrderCreateRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _orderService.CreateOrderAsync(request, cancellationToken);
            return CreatedAtAction(nameof(CreateOrder), new { id = response.OrderId }, response);
        }
        catch (OrderValidationException validationException)
        {
            _logger.LogWarning(validationException, "Validation failed for order creation.");
            return BadRequest(new ErrorResponse(validationException.Message));
        }
        catch (EntityNotFoundException entityNotFoundException)
        {
            _logger.LogWarning(entityNotFoundException, "Entity not found while creating order.");
            return NotFound(new ErrorResponse(entityNotFoundException.Message));
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while creating order.");
            return StatusCode(500, new ErrorResponse("An unexpected error occurred while processing the order."));
        }
    }
}
