namespace Api.Entities;

public class Series
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public double MinValue { get; set; }
    public double MaxValue { get; set; }
    /// <summary>"#RRGGBB".</summary>
    public required string Color { get; set; }
    public string? Icon { get; set; }
    public string? Unit { get; set; }

    public List<Sensor> Sensors { get; set; } = [];
}
