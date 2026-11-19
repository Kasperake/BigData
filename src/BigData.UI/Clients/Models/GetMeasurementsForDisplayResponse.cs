namespace BigData.UI.Clients.Models;

public sealed record GetMeasurementsForDisplayResponse
{
    public List<WeatherDto> Items { get; init; } = [];
}
