using MassTransit;
using MessageContracts.Messages.Invoice;
using Messaging.Handlers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using NUnit.Framework;
using PaymentMicroservice.Consumers;

namespace PaymentMicroservice.Tests.Integration;

[TestFixture]
public class PaymentIntegrationTests
{
    [Test]
    public async Task InvoiceCreated_Is_Handled_By_MessageHandler()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var rabbitSection = configuration.GetSection("RabbitMq");
        var hostName = rabbitSection.GetValue<string>("Host", "localhost");
        var queueBase = rabbitSection.GetValue<string>("QueueName", "payment-microservice");
        var exchangeName = rabbitSection.GetValue<string>("ExchangeName", "invoice-service");
        var exchangeType = rabbitSection.GetValue<string>("ExchangeType", "fanout");
        var queueName = $"{queueBase}-test-{Guid.NewGuid():N}";

        var handler = Substitute.For<IMessageHandler<InvoiceCreated>>();

        var host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddMassTransit(x =>
                {
                    x.AddConsumer<InvoiceCreatedConsumer>();
                    x.UsingRabbitMq((context, cfg) =>
                    {
                        cfg.Host(hostName);
                        cfg.ReceiveEndpoint(queueName, e =>
                        {
                            e.Bind(exchangeName, x => x.ExchangeType = exchangeType);
                            e.ConfigureConsumer<InvoiceCreatedConsumer>(context);
                        });
                    });
                });
                services.AddSingleton(handler);
                services.AddSingleton<IMessageHandler<InvoiceCreated>>(_ => handler);
            })
            .Build();

        await host.StartAsync();
        var bus = host.Services.GetRequiredService<IBus>();

        var message = new InvoiceCreated
        {
            InvoiceNumber = 123,
            InvoiceData = new InvoiceToCreate { CustomerNumber = 456 }
        };

        var sendEndpoint = await bus.GetSendEndpoint(new Uri($"exchange:{exchangeName}"));
        await sendEndpoint.Send(message);

        var handled = await WaitForCallAsync(() => handler.ReceivedCalls().Any(), TimeSpan.FromSeconds(2));
        await host.StopAsync();

        Assert.That(handled, Is.True, "Message handler did not receive the published message.");
        await handler.Received(1).Handle(Arg.Is<InvoiceCreated>(m => m.InvoiceNumber == 123), Arg.Any<CancellationToken>());
    }

    private static async Task<bool> WaitForCallAsync(Func<bool> condition, TimeSpan timeout)
    {
        var start = DateTime.UtcNow;
        while (DateTime.UtcNow - start < timeout)
        {
            if (condition()) return true;
            await Task.Delay(50);
        }
        return false;
    }
}
