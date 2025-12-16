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
[Category("Integration")]
public class PaymentIntegrationTests
{
    [SetUp]
    public void SetUp()
    {
        var configuration = new ConfigurationBuilder()
                           .AddJsonFile("appsettings.json", true)
                           .AddEnvironmentVariables()
                           .Build();

        var rabbitSection = configuration.GetSection("RabbitMq");
        _hostName = rabbitSection.GetValue<string>("Host", _hostName);
        _queueBase = rabbitSection.GetValue<string>("QueueName", _queueBase);
        _exchangeName = rabbitSection.GetValue<string>("ExchangeName", _exchangeName);
        _exchangeType = rabbitSection.GetValue<string>("ExchangeType", _exchangeType);
    }

    [TearDown]
    public async Task TearDown()
    {
        if (_host is not null)
        {
            await _host.StopAsync();
            await _host.WaitForShutdownAsync();
            _host.Dispose();
            _host = null;
        }

        _handler = null;
        _queueName = string.Empty;
    }

    private string _hostName = "localhost";
    private string _queueBase = "payment-microservice";
    private string _exchangeName = "invoice-service";
    private string _exchangeType = "fanout";
    private IHost? _host;
    private IMessageHandler<InvoiceCreated>? _handler;
    private string _queueName = string.Empty;

    [Test]
    public async Task InvoiceCreated_Is_Handled_By_MessageHandler()
    {
        _queueName = $"{_queueBase}-test-{Guid.NewGuid():N}";
        _handler = Substitute.For<IMessageHandler<InvoiceCreated>>();

        _host = Host.CreateDefaultBuilder()
                    .ConfigureServices(services =>
                     {
                         services.AddMassTransit(x =>
                         {
                             x.AddConsumer<InvoiceCreatedConsumer>();
                             x.UsingRabbitMq((context, cfg) =>
                             {
                                 cfg.Host(_hostName);
                                 cfg.ReceiveEndpoint(_queueName, e =>
                                 {
                                     e.Bind(_exchangeName, x => x.ExchangeType = _exchangeType);
                                     e.ConfigureConsumer<InvoiceCreatedConsumer>(context);
                                 });
                             });
                         });
                         services.AddSingleton(_handler);
                         services.AddSingleton<IMessageHandler<InvoiceCreated>>(_ => _handler);
                     })
                    .Build();

        await _host.StartAsync();
        var bus = _host.Services.GetRequiredService<IBus>();

        var message = new InvoiceCreated
        {
            InvoiceNumber = 123,
            InvoiceData = new InvoiceToCreate { CustomerNumber = 456 }
        };

        var sendEndpoint = await bus.GetSendEndpoint(new Uri($"exchange:{_exchangeName}"));
        await sendEndpoint.Send(message);

        var handled = await WaitForCallAsync(() => _handler!.ReceivedCalls().Any(), TimeSpan.FromSeconds(2));

        Assert.That(handled, Is.True, "Message handler did not receive the published message.");
        await _handler.Received(1)
                      .Handle(Arg.Is<InvoiceCreated>(m => m.InvoiceNumber == 123), Arg.Any<CancellationToken>());
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