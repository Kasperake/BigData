# Implementation: BigData.UI on the new display API + MudBlazor line chart

**Branch:** `feature/query-datawarehouse`
**Date:** 2026-11-19
**MudBlazor:** 9.14.0 (resolved from `9.*`)

---

## 1. Goal

The API no longer exposes paged raw Mongo observations. It now exposes data-warehouse measurements grouped per observation:

```
GET api/weather/GetForDisplay  →  GetMeasurementsForDisplayResponse
```

```jsonc
{
  "items": [
    {
      "stationId": "06180",
      "stationName": "København Lufthavn",
      "countryCode": "DNK",
      "observedAtUtc": "2026-11-18T12:00:00Z",
      "parameters": [
        { "type": "Temperature",   "value": 6.4 },
        { "type": "WindSpeed",     "value": 12.1 },
        { "type": "WindDirection", "value": 250 }
      ]
    }
  ]
}
```

`parameters[].type` is the `ParameterType` enum name as a **string** (`Temperature`, `WindSpeed`, `WindDirection`, `Pressure`). The handler sets it with `p.Type.ToString()` in `StormReadRepository.DisplayData`.

The UI must:
1. consume the new response, and
2. show the data in a **MudBlazor line chart**.

---

## 2. Changes

### 2.1 Client layer (`BigData.UI/Clients`)

| File | Change |
|------|--------|
| `IWeatherClient.cs` | `GetAsync(page, pageSize)` → `GetForDisplayAsync(ct)` |
| `WeatherClient.cs` | Calls `api/weather/GetForDisplay`; returns an empty response instead of `null` |
| `Models/GetMeasurementsForDisplayResponse.cs` | New, mirrors API (`List<WeatherDto> Items`) |
| `Models/WeatherDto.cs` | New: `StationId`, `StationName`, `CountryCode` (nullable), `ObservedAtUtc`, `Parameters` |
| `Models/ParameterDto.cs` | New: `Type` (string), `Value` |
| `WeatherApiOptions.cs` | Moved from `Models/` to namespace `BigData.UI.Clients` |
| `Models/GetWeatherObservations*.cs` | Deleted (old paged contract) |

The UI keeps `Type` as a `string` on purpose. That way the UI does not depend on enum ordinals, and a new parameter type such as `Pressure` shows up without breaking deserialization.

### 2.2 Chart profiles: `Components/Pages/Features/ParameterProfile.cs`

Every parameter has different units and axis rules, so these rules live in one record:

| Type | Label | Unit | Y axis | Drawing |
|------|-------|------|--------|---------|
| `Temperature` | Temperature | °C | auto (can be negative) | line |
| `WindSpeed` | Wind speed | m/s | starts at 0 | line |
| `WindDirection` | Wind direction | ° | 0 – 360 | **markers only, no line** |
| `Pressure` | Pressure | hPa | auto | line |
| *unknown* | type name | – | auto | line |

Wind direction is circular. A line from 350° to 10° would sweep down across the whole axis, so it is drawn as points (`ShowDataMarkers = true`, `LineStrokeWidth = 0`).

The grid columns, the details dialog, and the chart all use `ParameterProfile.For(type)`.

### 2.3 Line chart component: `Components/Pages/Features/MeasurementLineChart.razor`

Inputs: `Items` (`IReadOnlyList<WeatherDto>`) and `Profile` (`ParameterProfile`).

- Uses `MudChart<double>` with `ChartType.Timeseries` and `LineDisplayType.Line`. This is MudBlazor 9's line chart with a real `DateTime` X axis, so irregular sampling and gaps are spaced correctly. `ChartType.Line` only uses category labels and would space points evenly.
- It draws **one series per station**, so stations can be compared directly. `CanHideSeries="true"` lets the user toggle stations from the legend.
- Points are `TimeValue<double>(ObservedAtUtc, value)`. `ChartData<double>` has an implicit conversion from `List<TimeValue<double>>` (verified by reflection against 9.14.0).
- Options (`TimeSeriesChartOptions`):
  - `YAxisTitle` / `YAxisFormat` / `YAxisRequireZeroPoint` / `YAxisSuggestedMax` come from the profile
  - `TimeLabelSpacing` is chosen from the visible range (10 min to 30 d) to give about 6–12 labels
  - `TimeLabelFormat`: `HH:mm` for ≤ 1 day, otherwise `dd/MM HH:mm`, rotated 30°
  - `TooltipTimeLabelFormat = "yyyy-MM-dd HH:mm"`
- The subtitle shows the time range (UTC) and the number of observations.
- If there is no data for the selection, the chart shows an empty state.
- Series and options are rebuilt only when `Items` or `Profile` changes (a reference or value check in `OnParametersSet`), not on every render.

### 2.4 Page: `Components/Pages/Home.razor`

```
┌──────────────────────────────┬──────────────────────────────┐
│ Stations (multi-select)      │ Parameter (single select)    │
├──────────────────────────────┴──────────────────────────────┤
│ MeasurementLineChart  (one line per selected station)       │
├─────────────────────────────────────────────────────────────┤
│ MudDataGrid: Station | Id | Observed | <one col per param>  │
│                                               [Details]     │
└─────────────────────────────────────────────────────────────┘
```

- Loads data once in `OnInitializedAsync`. If an `HttpRequestException` occurs, it shows a snackbar error and the empty state, and the circuit stays up.
- The station and parameter lists come from the data. All stations are selected by default, and the default parameter is Temperature (or the first one available).
- Filtering by station creates a **new list instance**, so the chart's change detection picks it up.
- The grid changed from `ServerData` (server paging) to `Items` (client-side paging and sorting), because the new endpoint returns everything. It has one column per parameter type present in the data, sorted newest first.

### 2.5 Details dialog: `Components/Pages/Features/WeatherDetailsDialog.razor`

`Item` is now a `WeatherDto`. The dialog shows the station, station id, country code, observation time, and one read-only field per parameter (formatted with its profile).

### 2.6 API fix (required to run)

`BigData.API/Controllers/WeatherController.cs` still had `using BigData.API.Contracts;` and `using BigData.API.Mappers;` after commit `8eeb5a2 Removed unused mappings`. The API did not compile, so both lines were removed.

---

## 3. Verification

- `dotnet build src/BigData.UI` ✅
- `dotnet build src/BigData.API` ✅
- Manual check (not done yet; it needs SQL Server DW + API running):
  1. Start `BigData.API` and `BigData.UI` (`WeatherApi:BaseUrl` points at the API).
  2. `/` shows the selectors, the chart, and the grid.
  3. Switch the parameter to Temperature, Wind speed, and Wind direction. Check the units, the 0-based axis for wind speed, and the points-only drawing for wind direction.
  4. Deselect stations. The lines disappear and the grid filters.
  5. Stop the API. The page shows a snackbar error instead of crashing.

---

## 4. Known limitations / follow-ups

| # | Issue | Suggested follow-up |
|---|-------|---------------------|
| 1 | `GetForDisplay` returns **all** measurements, with no filter or paging. Payload size and SVG node count grow without bound. A 10-min sampling rate gives about 4,300 points per station per month. | Add `stationId`, `fromUtc`, `toUtc` (and optionally `maxPoints` with server-side bucketing) to `GetMeasurementsForDisplayQuery`, then add a date-range picker to the page. |
| 2 | `DisplayData` has no `ORDER BY` and no `AsNoTracking()`. | Add `.AsNoTracking()` and `.OrderBy(m => m.ObservedAtUtc)`. The UI sorts anyway. |
| 3 | Times are shown in UTC. | Convert to `Europe/Copenhagen` in one place (the chart's point builder), with a UTC toggle. |
| 4 | MudBlazor has no secondary Y axis, so the chart shows one parameter at a time. | Optional: show a small-multiples grid (one `MeasurementLineChart` per parameter). |
| 5 | Wind direction as points still loses the "circular" meaning. | Optional: add a wind rose (`ChartType.Rose`) next to it. |
| 6 | `MudBlazor` is referenced as `9.*`. | Pin to `9.14.*`. The v9 chart API has changed between minors. |
| 7 | There are no UI tests. | Add bUnit tests: `MeasurementLineChart` builds one series per station and shows the empty state. Also test the `ParameterProfile.For` fallback. |
