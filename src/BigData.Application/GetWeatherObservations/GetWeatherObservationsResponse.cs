namespace BigData.Application.GetWeatherObservations;

public sealed record GetWeatherObservationsResponse 
{
    public List<GetWeatherObservationsItem> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
}
