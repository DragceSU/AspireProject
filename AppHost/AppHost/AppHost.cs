using Projects;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<WebApi_Service>("webapi-service");
builder.AddProject<InvoiceMicroservice>("invoice-microservice");
builder.AddProject<PaymentMicroservice>("payment-microservice");

builder.Build().Run();
