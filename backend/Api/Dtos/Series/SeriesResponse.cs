namespace Api.Dtos.Series;

public record SeriesResponse(int Id, string Name, double MinValue, double MaxValue, string Color, string? Icon, string? Unit)
{
    public static SeriesResponse From(Entities.Series series) => new(
        series.Id, series.Name, series.MinValue, series.MaxValue, series.Color, series.Icon, series.Unit);
}
