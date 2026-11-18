using BigData.Domain.Entities.Enums;

namespace BigData.Domain.Entities
{
    public sealed class StormMeasurement
    {
        public StormMeasurement()
        {

        }
        public Guid Id { get; private set; }
        public string StationId { get; private set; } = null!;
        public DateTime ObservedAtUtc { get; private set; }
        public ICollection<Parameter> Parameters { get; private set; } = [];


        public static StormMeasurement Create(
            Guid id,
            string stationId,
            DateTime observedAtUtc,
            ICollection<Parameter> parameters
            )
        {
            return new()
            {
                Id = id,
                StationId = stationId,
                Parameters = parameters,
                ObservedAtUtc = observedAtUtc
            };
        }
    }
}
