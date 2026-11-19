using BigData.UI.Clients.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using MudBlazor.Charts;

namespace BigData.UI.Components.Pages.Features
{
    public partial class MeasurementLineChart
    {
        [Parameter, EditorRequired]
        public IReadOnlyList<WeatherDto> Items { get; set; } = [];

        [Parameter, EditorRequired]
        public ParameterProfile Profile { get; set; } = ParameterProfile.Temperature;

        private List<ChartSeries<double>> _series = [];
        private TimeSeriesChartOptions _options = new();
        private string _subtitle = string.Empty;

        // Only rebuild when the inputs actually change; building series on every render is wasteful.
        private IReadOnlyList<WeatherDto>? _lastItems;
        private ParameterProfile? _lastProfile;

        protected override void OnParametersSet()
        {
            if (ReferenceEquals(_lastItems, Items) && _lastProfile == Profile)
            {
                return;
            }

            _lastItems = Items;
            _lastProfile = Profile;
            Rebuild();
        }

        private void Rebuild()
        {
            var stations = Items
                .GroupBy(i => i.StationId ?? "unknown")
                .Select(g => new
                {
                    StationId = g.Key,
                    StationName = g.Select(i => i.StationName).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n)),
                    CountryCode = g.Select(i => i.CountryCode).FirstOrDefault(c => !string.IsNullOrWhiteSpace(c)),
                    Points = g
                        .SelectMany(m => m.Parameters
                            .Where(p => string.Equals(p.Type, Profile.Type, StringComparison.OrdinalIgnoreCase))
                            .Select(p => new TimeValue<double>(m.ObservedAtUtc, p.Value)))
                        .OrderBy(p => p.DateTime)
                        .ToList()
                })
                .Where(s => s.Points.Count > 0)
                .OrderBy(s => s.StationName ?? s.StationId)
                .ToList();

            _series = stations
                .Select(s => new ChartSeries<double>
                {
                    Name = LegendName(s.StationId, s.StationName, s.CountryCode),
                    Data = s.Points,
                    TooltipYValueFormat = Profile.ValueFormat
                })
                .ToList();

            if (stations.Count == 0)
            {
                _subtitle = string.Empty;
                return;
            }

            var from = stations.Min(s => s.Points[0].DateTime);
            var to = stations.Max(s => s.Points[^1].DateTime);
            var pointCount = stations.Sum(s => s.Points.Count);
            _subtitle = $"{from:yyyy-MM-dd HH:mm} – {to:yyyy-MM-dd HH:mm} UTC · {pointCount:N0} observations";

            _options = BuildOptions(to - from);
        }

        private TimeSeriesChartOptions BuildOptions(TimeSpan range)
        {
            var options = new TimeSeriesChartOptions
            {
                LineDisplayType = LineDisplayType.Line,
                YAxisTitle = Profile.Unit,
                YAxisFormat = Profile.ValueFormat,
                YAxisRequireZeroPoint = Profile.StartAtZero,
                YAxisSuggestedMax = Profile.SuggestedMax,
                YAxisLines = true,
                MaxNumYAxisTicks = 8,
                ShowLegend = true,
                TimeLabelSpacing = LabelSpacingFor(range),
                TimeLabelSpacingRounding = true,
                TimeLabelFormat = range <= TimeSpan.FromDays(1) ? "HH:mm" : "dd/MM HH:mm",
                TooltipTimeLabelFormat = "yyyy-MM-dd HH:mm",
                XAxisLabelRotation = range <= TimeSpan.FromDays(1) ? 0 : 30
            };

            if (Profile.MarkersOnly)
            {
                // Points without connecting lines (circular values must not be interpolated).
                options.ShowDataMarkers = true;
                options.LineStrokeWidth = 0;
            }

            return options;
        }

        /// <summary>Legend label, e.g. "København Lufthavn (06180, DNK)". Same format as the station picker.</summary>
        private static string LegendName(string stationId, string? stationName, string? countryCode)
        {
            var details = string.IsNullOrWhiteSpace(countryCode) ? stationId : $"{stationId}, {countryCode}";
            return string.IsNullOrWhiteSpace(stationName) ? details : $"{stationName} ({details})";
        }

        /// <summary>Picks a "nice" label spacing that yields roughly 6–12 labels on the X axis.</summary>
        private static TimeSpan LabelSpacingFor(TimeSpan range)
        {
            TimeSpan[] steps =
            [
                TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(30), TimeSpan.FromHours(1), TimeSpan.FromHours(3),
            TimeSpan.FromHours(6), TimeSpan.FromHours(12), TimeSpan.FromDays(1), TimeSpan.FromDays(7), TimeSpan.FromDays(30)
            ];

            return steps.FirstOrDefault(s => range / s <= 12, steps[^1]);
        }
    }
}