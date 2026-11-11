using Cqrs.Abstractions;

namespace BigData.Application.GetWeatherObservations;

public sealed record GetWeatherObservationsQuery(int Page = 1, int PageSize = 50) : IQuery<GetWeatherObservationsResponse>;
