namespace BigData.Domain.Entities;

public sealed class Station
{
    public Station()
    {
    }

    public Guid Id { get; private set; }
    public string StationId { get; private set; } = null!;
    public string CountryCode { get; private set; } = null!;
    public string Name { get; private set; } = null!;

    public static Station Create(Guid id, string stationId, string countryCode, string name)
    {
        return new()
        {
            Id = id,
            StationId = stationId,
            CountryCode = countryCode,
            Name = name
        };
    }

    public void Update(string countryCode, string name)
    {
        CountryCode = countryCode;
        Name = name;
    }
}
