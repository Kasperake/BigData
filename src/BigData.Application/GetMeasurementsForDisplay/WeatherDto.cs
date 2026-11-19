namespace BigData.Application.GetMeasurementsForDisplay;

public sealed record WeatherDto
{
    public string? StationId { get; set; }
    public string StationName { get; set; }
    public string? CountryCode { get; set; }
    public DateTime ObservedAtUtc { get; set; }
    public ICollection<ParameterDto> Parameters { get; set; }
}
