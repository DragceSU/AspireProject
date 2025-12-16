using MassTransit;
using MessageContracts.Messages.Invoice;
using Messaging.Producers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace InvoiceMicroservice.Tests.Integration;

[TestFixture]
[Category("Integration")]
public class InvoiceIntegrationTests
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

        _tcs?.TrySetCanceled();
        _tcs = null;
        _queueName = string.Empty;
    }

    private string _hostName = "localhost";
    private string _queueBase = "invoice-integration";
    private IHost? _host;
    private string _queueName = string.Empty;
    private TaskCompletionSource<InvoiceCreated>? _tcs;

    [Test]
    public async Task MessageProducer_Publishes_InvoiceCreated_To_Bus()
    {
        _queueName = $"{_queueBase}-test-{Guid.NewGuid():N}";
        _tcs = new TaskCompletionSource<InvoiceCreated>();

        _host = Host.CreateDefaultBuilder()
                    .ConfigureServices(services =>
                     {
                         services.AddMassTransit(x =>
                         {
                             x.UsingRabbitMq((context, cfg) =>
                             {
                                 cfg.Host(_hostName);
                                 cfg.ReceiveEndpoint(_queueName, e =>
                                 {
                                     e.Handler<InvoiceCreated>(context =>
                                     {
                                         _tcs.TrySetResult(context.Message);
                                         return Task.CompletedTask;
                                     });
                                 });
                             });
                         });
                     })
                    .Build();

        await _host.StartAsync();

        var publishEndpoint = _host.Services.GetRequiredService<IPublishEndpoint>();
        var producer =
            new MessageProducer<InvoiceCreated>(publishEndpoint, NullLogger<MessageProducer<InvoiceCreated>>.Instance);

        var message = new InvoiceCreated
        {
            InvoiceNumber = 789,
            InvoiceData = new InvoiceToCreate { CustomerNumber = 321 }
        };

        await producer.Produce(message, CancellationToken.None);

        var received = await Task.WhenAny(_tcs.Task, Task.Delay(TimeSpan.FromSeconds(2)));

        Assert.That(received, Is.EqualTo(_tcs.Task), "InvoiceCreated was not received by the in-memory endpoint.");
        Assert.That(_tcs.Task.Result.InvoiceNumber, Is.EqualTo(789));
    }
}