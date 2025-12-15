using MassTransit;
using MessageContracts.Messages.Invoice;
using Messaging.Producers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using InvoiceMicroservice;

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddJsonFile("appsettings.json", true, true);

builder.Services.Configure<AppConfiguration>(builder.Configuration);

builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        var options = context.GetRequiredService<IOptions<AppConfiguration>>().Value;
        var rabbitConfig = options.RabbitMq ?? new RabbitMqConfiguration();
        var hostName = string.IsNullOrWhiteSpace(rabbitConfig.Host) ? "localhost" : rabbitConfig.Host;

        cfg.Host(hostName);
    });
});

builder.Services.AddScoped(typeof(IMessageProducer<>), typeof(MessageProducer<>));
builder.Services.AddLogging();

var host = builder.Build();
await host.StartAsync();

var producer = host.Services.GetRequiredService<IMessageProducer<InvoiceCreated>>();
var exit = false;
while (!exit)
{
    Console.WriteLine("Press 'q' to exit or any other key to create an invoice.");
    var key = Console.ReadKey(true).Key;
    if (key == ConsoleKey.Q)
    {
        exit = true;
        continue;
    }

    var newInvoiceNumber = Random.Shared.Next(10000, 99999);
    Console.WriteLine($"Created invoice with number: {newInvoiceNumber}");

    InvoiceCreated invoiceCreated = new()
    {
        InvoiceNumber = newInvoiceNumber,
        MessageId = newInvoiceNumber.ToString(),
        InvoiceData = new InvoiceToCreate
        {
            MessageId = newInvoiceNumber.ToString(),
            CustomerNumber = 12345,
            InvoiceItems =
            [
                new InvoiceItems
                {
                    Description = "Item 1",
                    Price = 100.0,
                    ActualMileage = 50.0,
                    BaseRate = 10.0,
                    IsOversized = false,
                    IsRefrigerated = false,
                    IsHazardousMaterial = false,
                    MessageId = newInvoiceNumber.ToString()
                },
                new InvoiceItems
                {
                    Description = "Item 2",
                    Price = 200.0,
                    ActualMileage = 75.0,
                    BaseRate = 15.0,
                    IsOversized = true,
                    IsRefrigerated = false,
                    IsHazardousMaterial = true,
                    MessageId = newInvoiceNumber.ToString()
                }
            ]
        }
    };

    await producer.Produce(invoiceCreated, CancellationToken.None);
}

await host.StopAsync();
