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

// Secure by default: every controller action requires a valid token unless it opts out with [AllowAnonymous],
// so a new modifying endpoint can't be left unprotected by forgetting [Authorize]. (Scoped to controllers rather
// than a global fallback policy, so routing's own 404/405 responses stay public, as the contract expects.)
app.MapControllers().RequireAuthorization();

await app.Services.InitializeDatabaseAsync();

app.Run();
