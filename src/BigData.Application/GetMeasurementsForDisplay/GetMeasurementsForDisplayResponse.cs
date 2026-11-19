namespace BigData.Application.GetMeasurementsForDisplay;

public sealed record GetMeasurementsForDisplayResponse
{
    public List<WeatherDto> Items { get; init; } = [];
}

