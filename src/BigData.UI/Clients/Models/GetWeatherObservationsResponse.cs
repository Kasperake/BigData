namespace BigData.UI.Clients.Models
{
    public class GetWeatherObservationsResponse
    {
        public List<GetWeatherObservationsItem> Items { get; set; } = [];
        public int TotalPages { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
    }
}
