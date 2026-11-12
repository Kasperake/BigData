using BigData.API.Contracts;
using BigData.Application.GetWeatherObservations;

namespace BigData.API.Mappers;

public static class WeatherObservationsMapper
{
    public static GetWeatherObservationsApiResponse ToApiResponse(this GetWeatherObservationsResponse response) => new()
    {
        Items = response.Items.Select(ToApiItem).ToList(),
        Page = response.Page,
        PageSize = response.PageSize,
        TotalPages = response.TotalPages,
        TotalCount = response.TotalCount
    };

    private static WeatherObservationApiItem ToApiItem(GetWeatherObservationsItem item) => new()
    {
        Id = item.Id,
        StationId = item.StationId,
        ParameterId = item.ParameterId,
        ObservedAtUtc = item.ObservedAtUtc,
        CreatedAtUtc = item.CreatedAtUtc,
        Value = item.Value,
        Longitude = item.Longitude,
        Latitude = item.Latitude
    };
}
