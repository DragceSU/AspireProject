using MessageContracts.Messages;
using MessageContracts.Messages.Invoice;
using MessageContracts.Messages.Order;
using Microsoft.AspNetCore.Mvc;
using WebApi.Service.Dtos;
using WebApi.Service.Extensions;
using WebApi.Service.Services;
using WebApi.Service.Services.Producer;

namespace WebApi.Service.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly ILogger<OrdersController> _logger;
    private readonly IEnumerable<IMessageProducer<OrderSubmission>> _orderProducers;
    private readonly IEnumerable<IMessageProducer<InvoiceCreated>> _invoiceProducers;
    //private readonly IKafkaOrderProducer _kafkaOrderProducer;

    public OrdersController(ILogger<OrdersController> logger,
                            IEnumerable<IMessageProducer<OrderSubmission>> orderProducers, 
                            IEnumerable<IMessageProducer<InvoiceCreated>> invoiceProducers)
    //IKafkaOrderProducer kafkaOrderProducer)
    {
        _logger = logger;
        _orderProducers = orderProducers;
        _invoiceProducers = invoiceProducers;
        //_kafkaOrderProducer = kafkaOrderProducer;
    }

    /// <summary>
    /// Accepts an order payload from the storefront and acknowledges the submission.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OrderAcknowledgementDto>> Submit(OrderSubmissionDto order)
    {
        var validationResult = Validate(order);
        if (validationResult is not null) return validationResult;

        var acknowledgement = BuildAcknowledgement(order);
        _logger.LogInformation("Order {OrderId} received with {ItemCount} items (RabbitMQ).", order.OrderId, order.Items.Count);

        await _orderProducers.Single(x => x.Type == PublisherType.RabbitMq).Publish(order.ToOrderSubmissionMessage(), HttpContext.RequestAborted);

        return Accepted(acknowledgement);
    }

    /// <summary>
    /// Accepts an order payload and forwards it to Kafka.
    /// </summary>
    [HttpPost("kafka")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OrderAcknowledgementDto>> SubmitToKafka(OrderSubmissionDto order)
    {
        var validationResult = Validate(order);
        if (validationResult is not null) return validationResult;

        var acknowledgement = BuildAcknowledgement(order);
        _logger.LogInformation("Order {OrderId} received with {ItemCount} items (Kafka).", order.OrderId, order.Items.Count);

        await _orderProducers.Single(x => x.Type == PublisherType.Kafka).Publish(order.ToOrderSubmissionMessage(), HttpContext.RequestAborted);

        return Accepted(acknowledgement);
    }

    /// <summary>
    /// Accepts an order payload and forwards it to Kafka.
    /// </summary>
    [HttpPost(nameof(SubmitInvoiceToKafka))]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OrderAcknowledgementDto>> SubmitInvoiceToKafka(InvoiceCreated invoiceCreated)
    {
        //var validationResult = Validate(order);
        //if (validationResult is not null) return validationResult;

        //var acknowledgement = BuildAcknowledgement(invoiceCreated);
        //_logger.LogInformation("Order {OrderId} received with {ItemCount} items (Kafka).", order.OrderId, order.Items.Count);

        await _invoiceProducers.Single(x => x.Type == PublisherType.Kafka).Publish(invoiceCreated, HttpContext.RequestAborted);

        return Accepted();
    }

    private ActionResult<OrderAcknowledgementDto>? Validate(OrderSubmissionDto order)
    {
        if (order.Items.Count > 0) return null;

        ModelState.AddModelError(nameof(order.Items), "Provide at least one item.");
        return ValidationProblem(ModelState);
    }

    private static OrderAcknowledgementDto BuildAcknowledgement(OrderSubmissionDto order) =>
        new(
            order.OrderId,
            DateTimeOffset.UtcNow,
            order.Total,
            string.IsNullOrWhiteSpace(order.Currency) ? "USD" : order.Currency,
            "Received",
            order.Notes
        );
}
