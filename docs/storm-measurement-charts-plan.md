# Implementation Plan: StormMeasurement & Parameter Charts (MudBlazor)

**Branch:** `feature/add-datawarehouse`
**Date:** 2026-11-18
**MudBlazor version:** 9.14.0 (resolved from `MudBlazor 9.*` in `BigData.UI.csproj`)

---

## 1. Goal

Visualize data from the data warehouse (`WeatherDWDbContext.StormMeasurements` + their `Parameters`) in `BigData.UI` with MudBlazor charts. Each chart type must suit the measurement it shows:

| ParameterType   | Unit | Nature of data                  | Chart that fits                                                        |
|-----------------|------|---------------------------------|------------------------------------------------------------------------|
| `Temperature`   | °C   | Continuous, can be negative     | **Time series line** (mean), with optional min/max series per bucket |
| `WindSpeed`     | m/s  | Continuous, ≥ 0, storm thresholds | **Time series area/line** (mean + max), Y axis starts at 0, threshold reference lines |
| `WindDirection` | °    | **Circular** (0° = 360°)        | **Wind rose** (`ChartType.Rose`) of direction frequency + **scatter plot** of direction over time (no connecting lines) |

Why not one chart with all three: the units differ (°C, m/s, °), and MudBlazor has no secondary Y axis. Wind direction also must not be drawn as a connected line, because a shift from 350° to 10° would show as a false drop across the whole axis.

---

## 2. Current state (findings)

| Area | Observation | Impact |
|------|-------------|--------|
| Domain | `StormMeasurement { Id, StationId, ObservedAtUtc, Parameters }`, `Parameters { Id, Type, Value }`, `ParameterType { Temperature, WindDirection, WindSpeed }` | This is the data we chart. |
| Domain | `StormMeasurement.Create(...)` takes an unused `ParameterType type` argument | Cleanup (optional). |
| Application | `TransformMongoToSqlCommandHandler` builds entities but **never persists them**. The real logic lives in `MongoToSqlTransformer`, which the handler does not call. | ⚠️ **The DW may be empty.** This blocks the feature (see Phase 0). |
| Application | `IStormReadRepository` only has `GetExistingIdsAsync` | Needs read methods for charting. |
| Infrastructure | No EF migrations for `WeatherDWDbContext` (only `WeatherDbContext` has them) | DW schema/index must be created via migration. |
| Infrastructure | `DmiWeatherPressureImportWorker` imports pressure, but `MongoToSqlTransformer.TryMapParameterType` drops it (no `Pressure` enum value) | Out of scope. Design the chart profiles so new parameters can be added later. |
| API | Only `WeatherController` (paged raw observations from Mongo) | New controller required. |
| UI | MudBlazor 9 is installed. `MudThemeProvider` uses dark mode by default. `IWeatherClient` uses a typed HttpClient, and there is one page (`Home.razor`) | Follow the same client and page pattern. |

### MudBlazor 9 chart API (verified in 9.14.0 XML docs)
- Single generic component: `MudChart<T>` with `ChartType` = `Line | Timeseries | Bar | StackedBar | ScatterPlot | Rose | Radar | HeatMap | …`
- Series: `ChartSeries<T> { Name, Data (ChartData<T>), Visible, TooltipYValueFormat, … }`; time points: `TimeValue<T>(DateTime, T)`
- Options: `TimeSeriesChartOptions` (`TimeLabelSpacing`, `TimeLabelFormat`, `TooltipTimeLabelFormat`), `LineChartOptions` (`LineDisplayType.Area`, `SeriesDisplayOverrides`, `ClampToZero`, `ShowDataMarkers`), axis options (`YAxisTitle`, `YAxisFormat`, `YAxisSuggestedMax`, `YAxisRequireZeroPoint`, `MaxNumYAxisTicks`), `RoseChartOptions` (`AngleOffset`, `ShowValues`), `ScatterPlotChartOptions` (`PointRadius`, `XAxisFormat`)
- ⚠️ v9 changed the chart API compared to v7/v8, and most online samples still show `MudTimeSeriesChart` and `double[]` data. **Task 4.0 is a short spike** to confirm the exact binding for time-series data (`TimeValue<T>` vs `ChartData<T>` tuples) before building the components.

---

## 3. Architecture overview

```
BigData.UI  ──HTTP──▶  BigData.API  ──IMediator──▶  BigData.Application  ──▶  BigData.Infrastructure
  StormMeasurementsPage   StormMeasurementsController   GetStormMeasurementSeriesQuery   StormReadRepository (WeatherDWDbContext)
  Chart components        Contracts + Mapper            GetStormStationsQuery            
  IStormMeasurementClient                               SeriesAggregator (bucketing)
```

Key design decisions:
1. **Downsample on the server, not in the browser.** DMI delivers about 10-minute data (about 144 points/day per parameter). For 30 days that is about 4,300 points per series, which is too many SVG nodes for a Blazor Server circuit. The API returns bucketed data with at most **~500 points per series**.
2. **Return data in chart-ready long format** (one series per parameter with `(time, mean, min, max)` points), not entity graphs. The UI only maps DTO → `ChartSeries`.
3. **Use the correct aggregation per parameter:** arithmetic mean for temperature and wind speed. Wind direction uses the **circular mean** (`atan2(mean(sin), mean(cos))`), because the arithmetic mean of 350° and 10° is 180°, which is wrong.
4. **One "chart profile" per `ParameterType`** (unit, title, chart kind, axis range, format, color). This keeps the "fit to the measurement" logic in one place, and adding Pressure later only needs one new profile.

---

## 4. Phases & tasks

### Phase 0 — Prerequisites (data must reach the DW)
- [ ] **0.1** Make `TransformMongoToSqlCommandHandler` delegate to `IMongoToSqlTransformer.TransformAsync` (or persist through `IStormRepository` + a DW unit of work). The handler currently discards the entities.
- [ ] **0.2** Add an EF migration for `WeatherDWDbContext` (separate migrations folder, e.g. `Migrations/DataWarehouse`, using `--context WeatherDWDbContext --output-dir Migrations/DataWarehouse`).
- [ ] **0.3** In `StormMeasurementConfiguration`, add:
  - a unique index on `(StationId, ObservedAtUtc)`, which charting queries filter and sort on and the transformer uses for upserts
  - a configuration for `Parameters` (table name `Parameters`, index on `(StormMeasurementId, Type)`)
- [ ] **0.4** (Optional) Remove the unused `type` parameter from `StormMeasurement.Create`.

### Phase 1 — Application layer (CQRS queries)
New folder `BigData.Application/GetStormMeasurementSeries/`:

```csharp
public sealed record GetStormMeasurementSeriesQuery(
    string StationId,
    DateTime FromUtc,
    DateTime ToUtc,
    IReadOnlyCollection<ParameterType>? Types = null,   // null = all
    int MaxPointsPerSeries = 500) : IQuery<GetStormMeasurementSeriesResponse>;

public sealed record GetStormMeasurementSeriesResponse
{
    public string StationId { get; init; } = "";
    public DateTime FromUtc { get; init; }
    public DateTime ToUtc { get; init; }
    public TimeSpan BucketSize { get; init; }            // TimeSpan.Zero = raw data
    public List<ParameterSeries> Series { get; init; } = [];
}

public sealed record ParameterSeries(ParameterType Type, List<SeriesPoint> Points);
public sealed record SeriesPoint(DateTime TimeUtc, double Mean, double Min, double Max, int Count);
```

- [ ] **1.1** Query, response, and handler (`GetStormMeasurementSeriesQueryHandler`)
- [ ] **1.2** Extend `IStormReadRepository`:
  ```csharp
  Task<IReadOnlyList<ParameterValueRow>> GetParameterValuesAsync(
      string stationId, DateTime fromUtc, DateTime toUtc,
      IReadOnlyCollection<ParameterType>? types, CancellationToken ct);

  Task<IReadOnlyList<string>> GetStationIdsAsync(CancellationToken ct);
  ```
  `ParameterValueRow(DateTime ObservedAtUtc, ParameterType Type, double Value)` is a flat projection, with no entity tracking.
- [ ] **1.3** `SeriesAggregator` (a pure static class, easy to unit-test):
  - **Bucket size selection:** choose the smallest "nice" bucket (raw, 10 min, 30 min, 1 h, 3 h, 6 h, 12 h, 1 d) so that `range / bucket ≤ MaxPointsPerSeries`.
  - **Linear aggregate** (Temperature, WindSpeed): mean / min / max / count
  - **Circular aggregate** (WindDirection): circular mean normalized to [0, 360). Min and max are not meaningful here, so set both to the mean.
  - Aggregation is chosen by `ParameterType` through a small strategy map (`ParameterType → IAggregation`).
- [ ] **1.4** `GetStormStationsQuery` → `List<string>` station IDs that have DW data, used by the station picker. Names live in `WeatherDbContext.Stations`. Optionally enrich them in the API layer, but keep that out of the first iteration.
- [ ] **1.5** Validation: `FromUtc < ToUtc`, range ≤ 1 year, `StationId` required. Invalid input throws an existing or new validation exception, which `WeatherExceptionHandler` maps to 400.

> Aggregation runs in memory after a projected query, which is fine for ≤ 1 station × ≤ 1 year (about 160k rows). If that becomes too slow, move bucketing into SQL (`DATE_BUCKET` in SQL Server 2022+, or `DATEADD/DATEDIFF`).

### Phase 2 — Infrastructure
- [ ] **2.1** Implement the new methods in `StormReadRepository`:
  ```csharp
  dbContext.StormMeasurements.AsNoTracking()
      .Where(m => m.StationId == stationId && m.ObservedAtUtc >= fromUtc && m.ObservedAtUtc < toUtc)
      .SelectMany(m => m.Parameters, (m, p) => new ParameterValueRow(m.ObservedAtUtc, p.Type, p.Value))
      .Where(r => types == null || types.Contains(r.Type))
      .OrderBy(r => r.ObservedAtUtc)
      .ToListAsync(ct);
  ```
- [ ] **2.2** Check the generated SQL (it should be a single JOIN that uses the `(StationId, ObservedAtUtc)` index).

### Phase 3 — API
New `StormMeasurementsController` (`api/stormmeasurements`):

| Endpoint | Query params | Returns |
|----------|--------------|---------|
| `GET /api/stormmeasurements/series` | `stationId`, `from`, `to`, `types` (repeatable), `maxPoints` | `StormMeasurementSeriesApiResponse` |
| `GET /api/stormmeasurements/stations` | — | `string[]` |

- [ ] **3.1** Add contracts in `Contracts/StormMeasurementSeriesApiResponse.cs`. Serialize `ParameterType` as a **string** (`JsonStringEnumConverter`) so the UI does not depend on enum ordinals.
- [ ] **3.2** Add a mapper in `Mappers/StormMeasurementMapper.cs` (same pattern as `WeatherObservationsMapper`).
- [ ] **3.3** Check the endpoints manually in Scalar (`/scalar`).

### Phase 4 — UI (MudBlazor charts)

#### 4.0 Spike: confirm the v9 chart binding (about ½ day)
Build a throwaway page that renders the following with hard-coded data:
- `MudChart<double>` with `ChartType.Timeseries` and two series (mean and max)
- `ChartType.Rose` with 16 sectors
- `ChartType.ScatterPlot` with a time-based X axis (check whether X accepts `DateTime`/ticks, and how `XAxisFormat` formats it)

Write down the working snippets in this document. Fallback: if scatter cannot show time on X, use `Timeseries` with `ShowDataMarkers = true` and set the line stroke opacity to 0 with `SeriesDisplayOverrides`.

#### 4.1 Client
- [ ] `Clients/IStormMeasurementClient.cs` + `StormMeasurementClient.cs` (typed HttpClient, registered like `IWeatherClient` in `Program.cs`)
- [ ] Models: `StormMeasurementSeriesResponse`, `ParameterSeriesDto`, `SeriesPointDto`, `ParameterType` (UI copy of the enum, string-serialized)

#### 4.2 Chart profiles: "fit to the measurement"
`Components/Pages/StormMeasurements/ParameterChartProfiles.cs`

| Profile | Title | Chart | Y axis | Series | Extras |
|---------|-------|-------|--------|--------|--------|
| Temperature | "Temperature" | `Timeseries`, `LineDisplayType.Line` | `°C`, format `0.0`, **no** forced zero (`YAxisRequireZeroPoint = false`) | Mean (solid), Min/Max (thin and faded through `SeriesDisplayOverrides`, shown only when bucketed) | 0 °C reference line (optional) |
| Wind speed | "Wind speed" | `Timeseries`, `LineDisplayType.Area` | `m/s`, starts at 0 (`YAxisRequireZeroPoint = true`), `YAxisSuggestedMax = max(observed, 25)` | Mean (area), Max (line) | Reference lines at **17.2 m/s (gale, "hård kuling")**, **20.8 (strong gale)**, **24.5 (storm)**, drawn as constant series. Beaufort label in the tooltip |
| Wind direction | "Wind direction" | `Rose` (16 compass sectors, `AngleOffset` so N is at the top) + `ScatterPlot` over time | 0–360°, ticks every 90° (N/E/S/W via `YAxisToStringFunc`) | Rose: % of observations per sector. Scatter: direction per point | Optional: weight the rose by wind speed |

Each profile holds: `Type`, `Title`, `Unit`, `Icon`, `ChartType`, `Func<ChartOptions>`, `Func<ParameterSeriesDto, List<ChartSeries<double>>>` (the series builder), and a color from the theme palette (consistent in dark and light mode).

#### 4.3 Components
```
Components/Pages/StormMeasurements/
├── StormMeasurementsPage.razor          @page "/storm-measurements"
├── StormFilterBar.razor                 station select + date range + presets
├── StormSummaryCards.razor              KPI tiles
├── ParameterTimeSeriesChart.razor       generic, driven by a profile (Temperature, WindSpeed)
├── WindDirectionChart.razor             rose + scatter (tabs or side by side)
├── ParameterChartProfiles.cs
└── WindMath.cs                          sector binning, compass labels, Beaufort
```

- [ ] **StormFilterBar**: `MudSelect`/`MudAutocomplete` for the station (from `/stations`), `MudDateRangePicker`, and preset chips **24h / 7d / 30d / 1y**. A change raises `OnFilterChanged`. The last station is remembered in the query string (`?station=06180&range=7d`) so the view can be shared.
- [ ] **StormSummaryCards** (`MudPaper` tiles): latest temperature, min/max temperature in the range, max wind speed with its time, prevailing wind direction (compass label), and observation count.
- [ ] **ParameterTimeSeriesChart**: `MudCard` with a title and unit, a `MudChart` from the profile, an empty state ("No data for this period"), and a loading skeleton (`MudSkeleton`). It shows the bucket size in the subtitle ("hourly averages").
- [ ] **WindDirectionChart**: bins the points into 16 sectors client-side (`WindMath`) for the rose and uses the raw or bucketed points for the scatter.
- [ ] **Page layout** (`MudGrid`): filter bar (xs=12) → KPI row → Temperature (xs=12 md=6) | Wind speed (xs=12 md=6) → Wind direction (xs=12). Charts use `Width="100%"` and a fixed `Height="300px"` so they work on mobile.
- [ ] **Time zone**: data is UTC. Display it in local time (`Europe/Copenhagen`) and add a UTC toggle. Convert in one place (the series builder).
- [ ] Add `MudNavLink Href="storm-measurements" Icon="@Icons.Material.Filled.Air"` to `NavMenu.razor`.
- [ ] Errors: catch `HttpRequestException` → `ISnackbar` error and keep the last good data visible.
- [ ] Cancel in-flight requests when the filter changes (`CancellationTokenSource` per load). This matters in Blazor Server when users click through presets quickly.

#### 4.4 (Optional) Drill-down from Home
Add a "Charts" button in the `Home.razor` grid row that navigates to `/storm-measurements?station={StationId}&from=…`.

### Phase 5 — Tests
- [ ] **SeriesAggregator unit tests** (Application tests project, new if needed):
  - circular mean: `{350, 10}` → `0`, `{90, 270}` handled (undefined, document the behavior), `{0, 90}` → `45`
  - bucket selection: 1 day/500 → raw, 30 days/500 → 3 h, 1 year/500 → 1 d
  - min/max/count are correct, and empty input gives an empty series
- [ ] **Query handler tests** with a fake `IStormReadRepository`
- [ ] **WindMath tests**: sector binning edges (348.75°–11.25° → N), compass labels, Beaufort scale
- [ ] (Optional) **bUnit** test that `ParameterTimeSeriesChart` renders the empty state and builds the expected series from a profile

---

## 5. Acceptance criteria
1. `/storm-measurements` shows Temperature, Wind speed, and Wind direction for a selected station and period.
2. Each chart uses the right unit, axis range, and chart type: wind speed never goes below 0, and wind direction is never drawn as a connected line that wraps across 0°/360°.
3. Ranges from 24 h to 1 year render with ≤ 500 points per series and respond in < 1 s against a realistic DW.
4. The bucket size is shown to the user when data is aggregated.
5. Charts are readable in dark mode (default) and light mode.
6. Empty, loading, and error states are handled without breaking the circuit.
7. Aggregation logic is covered by unit tests.

---

## 6. Risks & mitigations
| Risk | Mitigation |
|------|-----------|
| DW is empty because the transform handler does not persist | Phase 0.1 must be done first |
| MudBlazor v9 API differs from online samples | Spike 4.0, and pin `MudBlazor` to `9.14.*` instead of `9.*` to avoid surprise breaking changes |
| Too many SVG points slow down the Blazor Server circuit | Server-side bucketing with `MaxPointsPerSeries` |
| Wrong wind direction averages | Circular mean plus unit tests |
| No secondary Y axis in MudBlazor | One chart per unit (by design) |
| Pressure is added later | Add an enum value, transformer mapping, and one `ParameterChartProfile` (line, hPa, no zero point) |

## 7. Estimate
| Phase | Effort |
|-------|--------|
| 0 Prerequisites | 0.5–1 d |
| 1 Application | 1 d |
| 2 Infrastructure | 0.5 d |
| 3 API | 0.5 d |
| 4 UI (incl. spike) | 2–2.5 d |
| 5 Tests | 1 d |
| **Total** | **≈ 5.5–6.5 days** |
