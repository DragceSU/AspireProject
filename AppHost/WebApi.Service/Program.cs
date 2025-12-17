using MassTransit;
using Messaging.Producers;
using Microsoft.Extensions.Options;
using WebApi.Service;
using WebApi.Service.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<AppConfiguration>(builder.Configuration);

builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        var options = context.GetRequiredService<IOptions<AppConfiguration>>().Value;
        var rabbitConfig = options.RabbitMq ?? new RabbitMqConfiguration();
        var hostName = ResolveRabbitHost(rabbitConfig.Host);

        cfg.Host(hostName);
    });
});

builder.Services.AddScoped(typeof(IMessageProducer<>), typeof(MessageProducer<>));
builder.Services.AddScoped<IOrderProducer, OrderProducer>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("WebAppCors", policy =>
    {
        var origins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? ["http://localhost:3000"];
        policy.WithOrigins(origins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.MapOpenApi();

app.UseHttpsRedirection();

app.UseAuthorization();
app.UseCors("WebAppCors");

app.MapControllers();

app.UseSwagger();

app.UseSwaggerUI();

app.Run();

static string ResolveRabbitHost(string? configuredHost)
{
    var hostName = string.IsNullOrWhiteSpace(configuredHost) ? "host.docker.internal" : configuredHost;
    if (!IsRunningInContainer())
        if (string.IsNullOrWhiteSpace(configuredHost) ||
            string.Equals(configuredHost, "host.docker.internal", StringComparison.OrdinalIgnoreCase))
            hostName = "localhost";

    return hostName;
}

static bool IsRunningInContainer()
{
    var aspireResource = Environment.GetEnvironmentVariable("ASPIRE_RESOURCE_NAME");
    if (!string.IsNullOrWhiteSpace(aspireResource)) return true;

    var runningInContainer = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER");
    return string.Equals(runningInContainer, "true", StringComparison.OrdinalIgnoreCase);
}

public partial class Program;