using System.Globalization;
using Api.Authentication;
using Api.Dtos.Measurements;
using Api.Errors;
using Api.Extensions;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/measurements")]
[Tags("measurements")]
public class MeasurementsController(IMeasurementService measurementService) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    [EndpointSummary("Wyniki wybranych serii w przedziale czasu")]
    [EndpointDescription("Wyniki posortowane rosnąco według timestamp (malejąco dla sort=-timestamp); przedział from ≤ timestamp ≤ to.")]
    [ProducesResponseType<IReadOnlyList<MeasurementResponse>>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ApiProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<ActionResult<IReadOnlyList<MeasurementResponse>>> Query(
        [FromQuery] MeasurementQuery query, CancellationToken cancellationToken)
    {
        var filter = new MeasurementFilter(
            query.SeriesIds(), query.FromValue(), query.ToValue(), query.IsDescending(), query.EffectiveLimit());
        var measurements = await measurementService.QueryAsync(filter, cancellationToken);
        return measurements.Select(MeasurementResponse.From).ToList();
    }

    [HttpGet("{id:long}")]
    [AllowAnonymous]
    [EndpointSummary("Odczyt wyniku")]
    [EndpointDescription("Wyników nie można modyfikować ani usuwać; metody PUT, PATCH i DELETE zwracają 405.")]
    [ProducesResponseType<MeasurementResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<MeasurementResponse>> Get(long id, CancellationToken cancellationToken)
    {
        var measurement = await measurementService.GetAsync(id, cancellationToken);
        return measurement is null ? NotFound() : MeasurementResponse.From(measurement);
    }

    [HttpPost]
    [Authorize(AuthenticationSchemes = ApiKeyDefaults.AuthenticationScheme)]
    [EndpointSummary("Przesłanie wyniku przez czujnik")]
    [EndpointDescription("Wymaga klucza API czujnika w nagłówku X-API-Key; seria wynika z klucza. Sam token administratora nie wystarcza.")]
    [ProducesResponseType<MeasurementResponse>(StatusCodes.Status201Created, "application/json")]
    [ProducesResponseType<ApiProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status415UnsupportedMediaType, "application/problem+json")]
    [ProducesResponseType<ApiProblemDetails>(StatusCodes.Status422UnprocessableEntity, "application/problem+json")]
    public async Task<ActionResult<MeasurementResponse>> Create(
        CreateMeasurementRequest request, CancellationToken cancellationToken)
    {
        if (User.GetSensorId() is not { } sensorId || User.GetSeriesId() is not { } seriesId)
            return Challenge(ApiKeyDefaults.AuthenticationScheme);

        var result = await measurementService.CreateAsync(
            sensorId, seriesId, request.Value!.Value, request.ParsedTimestamp(), cancellationToken);

        if (result.SensorGone)
            return Challenge(ApiKeyDefaults.AuthenticationScheme);
        if (result.Violations.Count > 0)
            return RuleViolations(result);

        var response = MeasurementResponse.From(result.Measurement!);
        return CreatedAtAction(nameof(Get), new { id = response.Id }, response);
    }

    private ObjectResult RuleViolations(CreateMeasurementResult result)
    {
        var series = result.Series!;
        var errors = result.Violations.Select(violation => violation switch
        {
            MeasurementRuleViolation.ValueOutOfRange => new ValidationError("value", string.Create(
                CultureInfo.InvariantCulture,
                $"Wartość musi należeć do przedziału serii [{series.MinValue}, {series.MaxValue}].")),
            MeasurementRuleViolation.TimestampTooFarInFuture => new ValidationError("timestamp",
                $"Znacznik czasu nie może być późniejszy niż {MeasurementRules.MaxFutureSkew.TotalMinutes} minut od bieżącego czasu serwera."),
            _ => throw new ArgumentOutOfRangeException(nameof(violation), violation, null),
        });

        return ValidationProblems.Create(HttpContext, StatusCodes.Status422UnprocessableEntity, "Dane nie spełniają reguł.", errors);
    }
}
