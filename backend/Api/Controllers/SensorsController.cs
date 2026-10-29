using Api.Dtos.Sensors;
using Api.Errors;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/sensors")]
[Tags("sensors")]
[Authorize]
public class SensorsController(ISensorService sensorService) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("Lista zarejestrowanych czujników (bez kluczy API)")]
    [ProducesResponseType<IReadOnlyList<SensorResponse>>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    public async Task<ActionResult<IReadOnlyList<SensorResponse>>> List(CancellationToken cancellationToken)
    {
        var sensors = await sensorService.ListAsync(cancellationToken);
        return sensors.Select(SensorResponse.From).ToList();
    }

    [HttpGet("{id:int}")]
    [EndpointSummary("Odczyt czujnika (bez klucza API)")]
    [ProducesResponseType<SensorResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<SensorResponse>> Get(int id, CancellationToken cancellationToken)
    {
        var sensor = await sensorService.GetAsync(id, cancellationToken);
        return sensor is null ? NotFound() : SensorResponse.From(sensor);
    }

    [HttpPost]
    [EndpointSummary("Rejestracja czujnika")]
    [EndpointDescription("Klucz API jest zwracany wyłącznie w tej odpowiedzi; serwer przechowuje tylko jego skrót.")]
    [ProducesResponseType<SensorCreatedResponse>(StatusCodes.Status201Created, "application/json")]
    [ProducesResponseType<ApiProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status415UnsupportedMediaType, "application/problem+json")]
    [ProducesResponseType<ApiProblemDetails>(StatusCodes.Status422UnprocessableEntity, "application/problem+json")]
    public async Task<ActionResult<SensorCreatedResponse>> Register(RegisterSensorRequest request, CancellationToken cancellationToken)
    {
        var registration = await sensorService.RegisterAsync(request.Name, request.SeriesId!.Value, cancellationToken);
        if (registration is null)
            return ValidationProblems.Unprocessable(HttpContext, "seriesId", "Seria o podanym identyfikatorze nie istnieje.");

        var response = SensorCreatedResponse.From(registration.Sensor, registration.ApiKey);
        // The body carries a one-time secret: make sure no cache along the way keeps it.
        Response.Headers.CacheControl = "no-store";
        return CreatedAtAction(nameof(Get), new { id = response.Id }, response);
    }

    [HttpDelete("{id:int}")]
    [EndpointSummary("Wyrejestrowanie czujnika")]
    [EndpointDescription("Klucz czujnika natychmiast przestaje działać; zebrane przez czujnik wyniki pozostają.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
        await sensorService.DeleteAsync(id, cancellationToken) ? NoContent() : NotFound();
}
