namespace BigData.UI.Components.Pages.Features;

/// <summary>
/// Describes how a measurement parameter (as returned by the API in <c>ParameterDto.Type</c>)
/// is labelled and charted. Keeps all "fit the chart to the measurement" rules in one place.
/// </summary>
/// <param name="Type">The parameter type string sent by the API (enum name, e.g. "Temperature").</param>
/// <param name="Label">Human readable name.</param>
/// <param name="Unit">Unit shown on the axis title and in the grid.</param>
/// <param name="ValueFormat">.NET number format used on the Y axis and in tooltips.</param>
/// <param name="StartAtZero">Force the Y axis to include 0 (values that can never be negative).</param>
/// <param name="SuggestedMax">Suggested upper bound for the Y axis.</param>
/// <param name="MarkersOnly">
/// Draw points without connecting lines. Used for circular values (wind direction) where a line
/// from 350° to 10° would wrongly sweep across the whole axis.
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

    /// <summary>Returns the profile for <paramref name="type"/>, or a generic line profile for unknown types.</summary>
    public static ParameterProfile For(string type) =>
        Known.TryGetValue(type, out var profile) ? profile : new ParameterProfile(type, type, string.Empty);

    public string DisplayName => string.IsNullOrEmpty(Unit) ? Label : $"{Label} ({Unit})";
}
