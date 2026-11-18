using BigData.Application.Repositories;
using Cqrs.Abstractions;

namespace BigData.Application.GetMeasurementsForDisplay
{
    internal class GetMeasurementsForDisplayQueryHandler(IStormReadRepository stormReadRepository) : IQueryHandler<GetMeasurementsForDisplayQuery, GetMeasurementsForDisplayResponse>
    {
        public async Task<GetMeasurementsForDisplayResponse> HandleAsync(GetMeasurementsForDisplayQuery query, CancellationToken cancellationToken)
        {
            var result = await stormReadRepository.DisplayData(cancellationToken);

            return new GetMeasurementsForDisplayResponse
            {
                Items = result
            };
        }
    }
}
