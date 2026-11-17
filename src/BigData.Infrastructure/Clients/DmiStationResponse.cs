namespace BigData.Infrastructure.Clients;

public sealed class DmiStationResponse
{
    public required List<DmiStationFeature> Features { get; init; }
}

public sealed class DmiStationFeature
{
    public required Guid Id { get; init; }
    public required DmiStationProperties Properties { get; init; }
}

public sealed class DmiStationProperties
{
    public required string StationId { get; init; }
    public string? Name { get; init; }
    public string? Country { get; init; }
    public DateTimeOffset? ValidFrom { get; init; }
    public DateTimeOffset? ValidTo { get; init; }
}
