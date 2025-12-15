var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var app = builder.Build();

app.MapOpenApi();

const string swaggerHtml = """
                           <!DOCTYPE html>
                           <html lang=\"en\">
                           <head>
                               <meta charset=\"utf-8\" />
                               <meta name=\"viewport\" content=\"width=device-width, initial-scale=1\" />
                               <title>Web API Swagger UI</title>
                               <link rel=\"stylesheet\" href=\"https://cdn.jsdelivr.net/npm/swagger-ui-dist@5/swagger-ui.css\" />
                           </head>
                           <body>
                               <div id=\"swagger-ui\"></div>
                               <script src=\"https://cdn.jsdelivr.net/npm/swagger-ui-dist@5/swagger-ui-bundle.js\"></script>
                               <script>
                                   window.onload = () => {
                                       SwaggerUIBundle({
                                           url: '/openapi/v1.json',
                                           dom_id: '#swagger-ui'
                                       });
                                   };
                               </script>
                           </body>
                           </html>
                           """;

app.MapGet("/swagger", () => Results.Content(swaggerHtml, "text/html"));

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program;