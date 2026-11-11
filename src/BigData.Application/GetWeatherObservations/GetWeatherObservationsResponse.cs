namespace BigData.Application.GetWeatherObservations;

public sealed record GetWeatherObservationsResponse 
{
    public List<GetWeatherObservationsItem> Items { get; set; } = [];
}
