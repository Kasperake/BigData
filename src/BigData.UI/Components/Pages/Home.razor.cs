using BigData.UI.Clients.Models;
using BigData.UI.Components.Pages.Features;
using MudBlazor;

namespace BigData.UI.Components.Pages
{
    public partial class Home
    {
        private sealed record StationOption(string Id, string Label, string Country);

        private sealed record StationRow(
            string StationId,
            string StationName,
            string? CountryCode,
            DateTime LatestObservedAtUtc,
            IReadOnlyList<WeatherDto> Measurements);

        private const string NoCountry = "Unknown";

        private bool _loading = true;
        private List<WeatherDto> _items = [];
        private List<WeatherDto> _filteredItems = [];
        private List<StationRow> _stationRows = [];
        private List<StationOption> _stations = [];
        private List<string> _countries = [];
        private List<ParameterProfile> _parameters = [];
        private IReadOnlyCollection<string> _selectedCountries = [];
        private IReadOnlyCollection<string> _selectedStations = [];

        private IEnumerable<StationOption> VisibleStations => _stations.Where(s => _selectedCountries.Contains(s.Country));
        private ParameterProfile _selectedParameter = ParameterProfile.Temperature;

        protected override async Task OnInitializedAsync()
        {
            try
            {
                var response = await WeatherClient.GetForDisplayAsync();
                _items = response.Items;
            }
            catch (HttpRequestException ex)
            {
                Snackbar.Add($"Could not load measurements: {ex.Message}", Severity.Error);
            }
            finally
            {
                _loading = false;
            }

            _stations = _items
                .GroupBy(i => i.StationId ?? "unknown")
                .Select(g =>
                {
                    var name = g.Select(i => i.StationName).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n));
                    var country = g.Select(i => i.CountryCode).FirstOrDefault(c => !string.IsNullOrWhiteSpace(c));
                    var details = country is null ? g.Key : $"{g.Key}, {country}";
                    return new StationOption(g.Key, name is null ? details : $"{name} ({details})", country ?? NoCountry);
                })
                .OrderBy(s => s.Label)
                .ToList();

            _countries = _stations.Select(s => s.Country).Distinct().OrderBy(c => c).ToList();

            _parameters = _items
                .SelectMany(i => i.Parameters)
                .Select(p => p.Type)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(ParameterProfile.For)
                .OrderBy(p => p.Label)
                .ToList();

            _selectedParameter = _parameters.FirstOrDefault(p => p == ParameterProfile.Temperature)
                                 ?? _parameters.FirstOrDefault()
                                 ?? ParameterProfile.Temperature;

            // Ved opstart vælges de 10 første stationer.
            _selectedCountries = _countries.ToList();
            _selectedStations = VisibleStations.Take(10).Select(s => s.Id).ToList();
            ApplyFilter();
        }

        private void OnCountriesChanged(IEnumerable<string?> values)
        {
            _selectedCountries = values.OfType<string>().ToList();
            _selectedStations = VisibleStations.Select(s => s.Id).ToList();
            ApplyFilter();
        }

        private void OnStationsChanged(IEnumerable<string?> values)
        {
            _selectedStations = values.OfType<string>().ToList();
            ApplyFilter();
        }

        private void OnParameterChanged(ParameterProfile profile) => _selectedParameter = profile;

        private void ApplyFilter()
        {
            var selected = _selectedStations.ToHashSet();
            // Ny liste instans, så diagramkomponenten registrerer ændringen.
            _filteredItems = _items.Where(i => selected.Contains(i.StationId ?? "unknown")).ToList();

            _stationRows = _filteredItems
                .GroupBy(i => i.StationId ?? "unknown")
                .Select(g =>
                {
                    // Målinger observeret på samme tid, bliver grupperet ind på samme linje.
                    var measurements = g
                        .GroupBy(i => i.ObservedAtUtc)
                        .Select(t => t.First() with
                        {
                            Parameters = t
                                .SelectMany(i => i.Parameters)
                                .GroupBy(p => p.Type, StringComparer.OrdinalIgnoreCase)
                                .Select(p => p.First())
                                .ToList()
                        })
                        .OrderByDescending(i => i.ObservedAtUtc)
                        .ToList();
                    return new StationRow(
                        g.Key,
                        measurements.Select(i => i.StationName).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n)) ?? g.Key,
                        measurements.Select(i => i.CountryCode).FirstOrDefault(c => !string.IsNullOrWhiteSpace(c)),
                        measurements[0].ObservedAtUtc,
                        measurements);
                })
                .ToList();
        }

        private static string FormatLatestValue(StationRow row, ParameterProfile profile)
        {
            var latest = row.Measurements.FirstOrDefault(m =>
                m.Parameters.Any(p => string.Equals(p.Type, profile.Type, StringComparison.OrdinalIgnoreCase)));
            return latest is null ? "–" : FormatValue(latest, profile);
        }

        private string StationLabel(string? id) =>
            _stations.FirstOrDefault(s => s.Id == id)?.Label ?? id ?? string.Empty;

        private static string FormatValue(WeatherDto item, ParameterProfile profile)
        {
            var parameter = item.Parameters.FirstOrDefault(p => string.Equals(p.Type, profile.Type, StringComparison.OrdinalIgnoreCase));
            return parameter is null ? "–" : parameter.Value.ToString(profile.ValueFormat);
        }

        private Task OpenDialogAsync(WeatherDto item)
        {
            var options = new DialogOptions { CloseOnEscapeKey = true };

            var parameters = new DialogParameters<WeatherDetailsDialog>
        {
            { x => x.Item, item }
        };

            return DialogService.ShowAsync<WeatherDetailsDialog>("Weather Details", parameters, options);
        }
    }
}