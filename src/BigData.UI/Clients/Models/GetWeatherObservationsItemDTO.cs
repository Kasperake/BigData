namespace BigData.UI.Clients.Models
{
    public class GetWeatherObservationsItemDTO
    {
        public List<GetWeatherObservationsResponseDTO> Items { get; set; } = [];
        public int Page { get; set; }
        public int PageSize { get; set; }
    }
}
