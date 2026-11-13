namespace BigData.UI.Clients.Models
{
    public class WeatherApiOptions
    {
        public const string SectionName = "WeatherApi";
        public required Uri BaseUrl { get; set; }
    }
}
