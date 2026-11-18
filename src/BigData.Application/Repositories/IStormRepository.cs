using BigData.Domain.Entities;

namespace BigData.Application.Repositories;

public interface IStormRepository
{
    Task Add(StormMeasurement entity, CancellationToken cancellationToken = default);
}