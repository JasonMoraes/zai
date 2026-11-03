using Api.Data;
using Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApiControllers()
    .AddDatabase()
    .AddJwtAuthentication()
    .AddApplicationServices();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger(options => options.RouteTemplate = "api/swagger/{documentName}/swagger.json");
    app.UseSwaggerUI(options =>
    {
        options.RoutePrefix = "api/swagger";
        options.SwaggerEndpoint("v1/swagger.json", "API v1");
    });
}

app.UseAuthentication();
app.UseAuthorization();

// Actions without their own [Authorize…]/[AllowAnonymous] require an admin token: see AuthorizeByDefaultConvention.
app.MapControllers();

await app.Services.InitializeDatabaseAsync();

app.Run();
