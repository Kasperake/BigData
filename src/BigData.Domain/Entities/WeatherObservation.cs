namespace BigData.Domain.Entities;

public sealed class WeatherObservation
{
    public WeatherObservation()
    {
        
    }


    public Guid Id { get; private set; }
    public string StationId { get; private set; } = null!;
    public string ParameterId { get; private set; } = null!;
    public DateTime ObservedAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public double Value { get; private set; }
    public double? Longitude { get; private set; }
    public double? Latitude { get; private set; }


    public static WeatherObservation Create(
        Guid id,
        string stationId,
        string parameterId,
        DateTime observedAtUtc,
        DateTime createdAtUtc,
        double value,
        double? longitude,
        double? latitude)
    {
        return new()
        {
            Id = id,
            StationId = stationId,
            ParameterId = parameterId,
            ObservedAtUtc = observedAtUtc,
            CreatedAtUtc = createdAtUtc,
            Value = value,
            Longitude = longitude,
            Latitude = latitude
        };
    }
}
