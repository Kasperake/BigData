namespace BigData.Application.Clients;

public sealed record StationInput(
    Guid Id,
    string StationId,
    string CountryCode,
    string Name);
