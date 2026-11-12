namespace BigData.Infrastructure.Clients;

public class DmiWeatherClientOptions
{
    public const string SectionName = "DmiWeatherClient";

    public string BaseAddress { get; set; } = "https://opendataapi.dmi.dk/v2/metObs/";
}
