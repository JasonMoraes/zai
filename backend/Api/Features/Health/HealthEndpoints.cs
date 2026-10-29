namespace Api.Features.Health;

public record HealthResponse(string Status);

public static class HealthEndpoints
{
    public static RouteGroupBuilder MapHealthEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/health", () => TypedResults.Ok(new HealthResponse("ok")))
            .WithTags("system")
            .WithSummary("Sprawdzenie dostępności aplikacji")
            .WithDescription("Używane przez testy do „wybudzenia” aplikacji na darmowym hostingu.");

        return api;
    }
}
