using BigData.Infrastructure.Persistence;
using Cqrs.Abstractions;

namespace BigData.Application.GetWeatherObservations;

public class GetWeatherObservationsQueryHandler(IWeatherObservationReadRepository readRepository) : IQueryHandler<GetWeatherObservationsQuery, GetWeatherObservationsResponse>
{
    public async Task<GetWeatherObservationsResponse> HandleAsync(GetWeatherObservationsQuery query, CancellationToken cancellationToken)
    {
        var totalCount = await readRepository.CountAsync(cancellationToken);
        var items = await readRepository.GetAllAsync(query.Page, query.PageSize, cancellationToken);

        return new GetWeatherObservationsResponse
        {
            Items = items,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount,
            TotalPages = query.PageSize > 0 ? (int)Math.Ceiling(totalCount / (double)query.PageSize) : 0
        };
    }
}
