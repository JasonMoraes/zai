using Api.Dtos.Series;
using Api.Errors;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/series")]
[Tags("series")]
public class SeriesController(ISeriesService seriesService) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    [EndpointSummary("Lista serii")]
    [ProducesResponseType<IReadOnlyList<SeriesResponse>>(StatusCodes.Status200OK, "application/json")]
    public async Task<ActionResult<IReadOnlyList<SeriesResponse>>> List(CancellationToken cancellationToken)
    {
        var series = await seriesService.ListAsync(cancellationToken);
        return series.Select(SeriesResponse.From).ToList();
    }

    [HttpGet("{seriesId:int}")]
    [AllowAnonymous]
    [EndpointSummary("Odczyt serii")]
    [ProducesResponseType<SeriesResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<SeriesResponse>> Get(int seriesId, CancellationToken cancellationToken)
    {
        var series = await seriesService.GetAsync(seriesId, cancellationToken);
        return series is null ? NotFound() : SeriesResponse.From(series);
    }

    [HttpPost]
    [Authorize]
    [EndpointSummary("Utworzenie serii")]
    [ProducesResponseType<SeriesResponse>(StatusCodes.Status201Created, "application/json")]
    [ProducesResponseType<ApiProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status415UnsupportedMediaType, "application/problem+json")]
    [ProducesResponseType<ApiProblemDetails>(StatusCodes.Status422UnprocessableEntity, "application/problem+json")]
    public async Task<ActionResult<SeriesResponse>> Create(SeriesRequest request, CancellationToken cancellationToken)
    {
        var result = await seriesService.CreateAsync(ToValues(request), cancellationToken);
        if (result.Status == SeriesWriteStatus.InvalidRange)
            return InvalidRangeProblem();

        var response = SeriesResponse.From(result.Series!);
        return CreatedAtAction(nameof(Get), new { seriesId = response.Id }, response);
    }

    [HttpPut("{seriesId:int}")]
    [Authorize]
    [EndpointSummary("Pełna aktualizacja serii")]
    [EndpointDescription("Zastępuje wszystkie edytowalne pola serii. Przedział, który nie obejmuje zapisanych wyników, jest odrzucany kodem 409.")]
    [ProducesResponseType<SeriesResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ApiProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    [ProducesResponseType<ApiProblemDetails>(StatusCodes.Status422UnprocessableEntity, "application/problem+json")]
    public async Task<ActionResult<SeriesResponse>> Update(
        int seriesId, SeriesRequest request, CancellationToken cancellationToken)
    {
        var result = await seriesService.UpdateAsync(seriesId, ToValues(request), cancellationToken);
        return result.Status switch
        {
            SeriesWriteStatus.Done => SeriesResponse.From(result.Series!),
            SeriesWriteStatus.InvalidRange => InvalidRangeProblem(),
            SeriesWriteStatus.NotFound => NotFound(),
            SeriesWriteStatus.ExcludesSavedMeasurements => Problem(
                statusCode: StatusCodes.Status409Conflict,
                detail: "Nowy przedział [minValue, maxValue] nie obejmuje wszystkich wyników zapisanych w serii."),
            _ => throw new ArgumentOutOfRangeException(nameof(result), result.Status, null),
        };
    }

    [HttpDelete("{seriesId:int}")]
    [Authorize]
    [EndpointSummary("Usunięcie serii wraz ze wszystkimi jej wynikami i przypisanymi czujnikami")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> Delete(int seriesId, CancellationToken cancellationToken) =>
        await seriesService.DeleteAsync(seriesId, cancellationToken) ? NoContent() : NotFound();

    private static SeriesValues ToValues(SeriesRequest request) => new(
        request.Name, request.MinValue!.Value, request.MaxValue!.Value, request.Color, request.Icon, request.Unit);

    private ObjectResult InvalidRangeProblem() =>
        ValidationProblems.Unprocessable(HttpContext, "minValue", "Wartość minValue musi być mniejsza niż maxValue.");
}
