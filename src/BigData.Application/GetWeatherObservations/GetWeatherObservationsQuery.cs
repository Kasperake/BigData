using Cqrs.Abstractions;

namespace BigData.Application.GetWeatherObservations;

public sealed record GetWeatherObservationsQuery : IQuery<GetWeatherObservationsResponse>;
