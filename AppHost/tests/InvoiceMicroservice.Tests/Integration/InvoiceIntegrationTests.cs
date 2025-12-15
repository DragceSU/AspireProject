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
public class InvoiceIntegrationTests
{
    [Test]
    public async Task MessageProducer_Publishes_InvoiceCreated_To_Bus()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var rabbitSection = configuration.GetSection("RabbitMq");
        var hostName = rabbitSection.GetValue<string>("Host", "localhost");
        var queueBase = rabbitSection.GetValue<string>("QueueName", "invoice-integration");
        var queueName = $"{queueBase}-test-{Guid.NewGuid():N}";

        var tcs = new TaskCompletionSource<InvoiceCreated>();

        var host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddMassTransit(x =>
                {
                    x.UsingRabbitMq((context, cfg) =>
                    {
                        cfg.Host(hostName);
                        cfg.ReceiveEndpoint(queueName, e =>
                        {
                            e.Handler<InvoiceCreated>(context =>
                            {
                                tcs.TrySetResult(context.Message);
                                return Task.CompletedTask;
                            });
                        });
                    });
                });
            })
            .Build();

        await host.StartAsync();

        var publishEndpoint = host.Services.GetRequiredService<IPublishEndpoint>();
        var producer = new MessageProducer<InvoiceCreated>(publishEndpoint, NullLogger<MessageProducer<InvoiceCreated>>.Instance);

        var message = new InvoiceCreated
        {
            InvoiceNumber = 789,
            InvoiceData = new InvoiceToCreate { CustomerNumber = 321 }
        };

        await producer.Produce(message, CancellationToken.None);

        var received = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(2)));
        await host.StopAsync();

        Assert.That(received, Is.EqualTo(tcs.Task), "InvoiceCreated was not received by the in-memory endpoint.");
        Assert.That(tcs.Task.Result.InvoiceNumber, Is.EqualTo(789));
    }
}
