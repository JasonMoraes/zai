using Api.Dtos.Health;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/health")]
[AllowAnonymous]
[Tags("system")]
public class HealthController : ControllerBase
{
    [HttpGet]
    [EndpointSummary("Sprawdzenie dostępności aplikacji")]
    [EndpointDescription("Używane przez testy do „wybudzenia” aplikacji na darmowym hostingu.")]
    [ProducesResponseType<HealthResponse>(StatusCodes.Status200OK, "application/json")]
    public ActionResult<HealthResponse> Get() => new HealthResponse("ok");
}
