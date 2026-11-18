namespace BigData.Application.GetMeasurementsForDisplay
{
    public class WeatherDto
    {
        public string? StationId { get; set; }
        public string StationName { get; set; }
        public DateTime ObservedAtUtc { get; set; }
        public ICollection<ParameterDto> Parameters { get; set; }
    }
}
