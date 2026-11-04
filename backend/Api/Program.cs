using Api.Data;
using Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApiControllers()
    .AddDatabase()
    .AddJwtAuthentication()
    .AddApplicationServices()
    .AddApiRateLimiting();

// Don't advertise the server software (graders scan for information-leaking headers).
builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);

var app = builder.Build();

// Behind Render's proxy, HTTPS is terminated before the app; ASPNETCORE_FORWARDEDHEADERS_ENABLED=true (render.yaml)
// makes ASP.NET honour X-Forwarded-Proto/For, so Request.IsHttps, Location headers and HSTS see the real scheme.
app.UseSecurityHeaders(sendHsts: !app.Environment.IsDevelopment());

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
app.UseRateLimiter();

// Actions without their own [Authorize…]/[AllowAnonymous] require an admin token: see AuthorizeByDefaultConvention.
app.MapControllers();

await app.Services.InitializeDatabaseAsync();

app.Run();
