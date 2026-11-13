namespace BigData.UI.Clients.Models
{
    public class GetWeatherObservationsItem
    {
        public Guid Id { get; set; }
        public string StationId { get; set; }
        public string ParameterId { get; set; }
        public DateTime ObservedAtUtc { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public double Value { get; set; }
        public double? Longitude { get; set; }
        public double? Latitude { get; set; }
    }
}
