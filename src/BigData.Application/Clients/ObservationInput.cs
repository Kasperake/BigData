namespace BigData.Application.Clients;

public sealed record ObservationInput(
    Guid Id,
    string StationId,
    string ParameterId,
    DateTime ObservedAtUtc,
    DateTime CreatedAtUtc,
    double Value,
    double? Longitude,
    double? Latitude);
