namespace BigData.Domain.Entities;

public sealed class WeatherObservation
{
    public WeatherObservation()
    {
        
    }


    public Guid Id { get; set; }
    public string StationId { get;  set; } = null!;
    public string ParameterId { get; set; } = null!;
    public DateTime ObservedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public double Value { get; set; }
    public double? Longitude { get;  set; }
    public double? Latitude { get; set; }


    public static WeatherObservation Create(
        Guid id,
        string stationId,
        string parameterId,
        DateTime observedAtUtc,
        DateTime createdAtUtc,
        float value,
        float? longitude,
        float? latitude)
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
