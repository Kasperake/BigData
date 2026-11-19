using Cqrs.Abstractions;

namespace BigData.Application.GetMeasurementsForDisplay;

public sealed record GetMeasurementsForDisplayQuery : IQuery<GetMeasurementsForDisplayResponse>;
