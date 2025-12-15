using Projects;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<WebApi_Service>("webapi-service");

builder.Build().Run();