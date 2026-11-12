namespace BigData.Infrastructure.Clients;

public sealed class DmiResponse
{
    public required List<DmiFeature> Features { get; init; }
    public List<DmiLink> Links { get; init; } = [];
}

public sealed class DmiFeature
{
    public required Guid Id { get; init; }
    public DmiGeometry? Geometry { get; init; }
    public required DmiProperties Properties { get; init; }
}

public sealed class DmiGeometry
{
    public double[]? Coordinates { get; init; }
}

public sealed class DmiProperties
{
    public required string StationId { get; init; }
    public required string ParameterId { get; init; }
    public required DateTimeOffset Observed { get; init; }
    public required DateTimeOffset Created { get; init; }
    public required double Value { get; init; }
}

public sealed class DmiLink
{
    public required string Rel { get; init; }
    public required string Href { get; init; }
}