namespace BigData.API.Contracts;

public sealed record GetWeatherObservationsApiResponse
{
    public List<WeatherObservationApiItem> Items { get; init; } = [];
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }
    public int TotalCount { get; init; }
}

public sealed record WeatherObservationApiItem
{
    public Guid Id { get; init; }
    public string StationId { get; init; } = string.Empty;
    public string ParameterId { get; init; } = string.Empty;
    public DateTime ObservedAtUtc { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public double Value { get; init; }
    public double? Longitude { get; init; }
    public double? Latitude { get; init; }
}
