using Microsoft.AspNetCore.Mvc;
using WebApi.Service.Dtos;
using WebApi.Service.Services;

namespace WebApi.Service.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly ILogger<OrdersController> _logger;
    private readonly IOrderProducer _orderProducer;

    public OrdersController(ILogger<OrdersController> logger, IOrderProducer orderProducer)
    {
        _logger = logger;
        _orderProducer = orderProducer;
    }

    /// <summary>
    /// Accepts an order payload from the storefront and acknowledges the submission.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OrderAcknowledgementDto>> Submit(OrderSubmissionDto order)
    {
        if (order.Items.Count == 0)
        {
            ModelState.AddModelError(nameof(order.Items), "Provide at least one item.");
            return ValidationProblem(ModelState);
        }

        var acknowledgement = new OrderAcknowledgementDto(
            order.OrderId,
            DateTimeOffset.UtcNow,
            order.Total,
            string.IsNullOrWhiteSpace(order.Currency) ? "USD" : order.Currency,
            "Received",
            order.Notes
        );

        _logger.LogInformation("Order {OrderId} received with {ItemCount} items.", order.OrderId, order.Items.Count);

        await _orderProducer.Publish(order, HttpContext.RequestAborted);

        return Accepted(acknowledgement);
    }
}
