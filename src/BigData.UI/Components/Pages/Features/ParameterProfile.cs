namespace BigData.UI.Components.Pages.Features;

/// <param name="Type">Det navn API'en sender</param>
/// <param name="Label">Det navn brugeren læser</param>
/// <param name="Unit">Den unit der bliver angivet i vores chart</param>
/// <param name="ValueFormat">Talformat brugt på Y-aksen og i tooltips.</param>
/// <param name="StartAtZero">Tving Y-aksen til at inkludere 0 (værdier, der aldrig kan være negative).</param>
/// <param name="SuggestedMax">Foreslået øvre grænse for Y-aksen.</param>
/// <param name="MarkersOnly">
/// Tegn punkter uden forbindelseslinjer. Bruges til cirkulære værdier (vindretning), hvor en linje
/// fra 350° til 10° forkert ville feje over hele aksen.
/// </param>
public sealed record ParameterProfile(
    string Type,
    string Label,
    string Unit,
    string ValueFormat = "0.0",
    bool StartAtZero = false,
    double? SuggestedMax = null,
    bool MarkersOnly = false)
{
    public static readonly ParameterProfile Temperature = new("Temperature", "Temperature", "°C");
    public static readonly ParameterProfile WindSpeed = new("WindSpeed", "Wind speed", "m/s", StartAtZero: true);
    public static readonly ParameterProfile WindDirection = new("WindDirection", "Wind direction", "°", "0", StartAtZero: true, SuggestedMax: 360, MarkersOnly: true);
    public static readonly ParameterProfile Pressure = new("Pressure", "Pressure", "hPa", "0");

    private static readonly Dictionary<string, ParameterProfile> Known =
        new[] { Temperature, WindSpeed, WindDirection, Pressure }
            .ToDictionary(p => p.Type, StringComparer.OrdinalIgnoreCase);

    /// <summary>Returnerer profilen "for" <paramref name="type"/>, eller hvis ukendt, en generisk profil.</summary>
    public static ParameterProfile For(string type) =>
        Known.TryGetValue(type, out var profile) ? profile : new ParameterProfile(type, type, string.Empty);

    public string DisplayName => string.IsNullOrEmpty(Unit) ? Label : $"{Label} ({Unit})";
}
