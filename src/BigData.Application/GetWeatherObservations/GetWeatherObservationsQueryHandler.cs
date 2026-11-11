using BigData.Infrastructure.Persistence;
using Cqrs.Abstractions;

namespace BigData.Application.GetWeatherObservations;

public class GetWeatherObservationsQueryHandler(IWeatherObservationReadRepository readRepository) : IQueryHandler<GetWeatherObservationsQuery, GetWeatherObservationsResponse>
{
    public async Task<GetWeatherObservationsResponse> HandleAsync(GetWeatherObservationsQuery query, CancellationToken cancellationToken)
    {
        return new GetWeatherObservationsResponse
        {
            Items = await readRepository.GetAllAsync(cancellationToken)
        };
    }
}
