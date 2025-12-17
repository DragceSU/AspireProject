using MessageContracts.Messages.Order;
using Messaging.Producers;
using WebApi.Service.Dtos;
using WebApi.Service.Extensions;

namespace WebApi.Service.Services;

public interface IOrderProducer
{
    Task Publish(OrderSubmissionDto dto, CancellationToken cancellationToken);
}

public class OrderProducer(IMessageProducer<OrderSubmission> producer) : IOrderProducer
{
    public Task Publish(OrderSubmissionDto dto, CancellationToken cancellationToken)
    {
        var message = dto.ToOrderSubmissionMessage();

        return producer.Produce(message, cancellationToken);
    }
}
