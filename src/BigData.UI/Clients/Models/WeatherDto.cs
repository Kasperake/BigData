namespace BigData.UI.Clients.Models;

public sealed record WeatherDto
{
    public string? StationId { get; set; }
    public string StationName { get; set; } = string.Empty;
    public DateTime ObservedAtUtc { get; set; }
    public ICollection<ParameterDto> Parameters { get; set; } = [];
}
