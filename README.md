# BigData

BigData er et projekt, der kan extracte, transformere, loade og visualisere vejrmålinger, såsom temperatur, vindhastighed og lufttryk.

## Solution overview

Projektet er struktureret efter DDD-principper, og er opbygget på denne måde:

- `src/BigData.API` – REST API (controllers, OpenAPI, Scalar, exception handling)
- `src/BigData.UI` – Blazor Server UI - der går brug af MudBlazor's component library.
- `src/BigData.Application` – Application use cases og CQRS handlers
- `src/BigData.Domain` – Domain entities og kontrakter
- `src/BigData.Infrastructure` – Persistence og workers
- `lib/Cqrs/*` – Et eksternt library - CQRS abstractions/infrastructure

## Tech stack

- .NET 10
- ASP.NET Core Web API
- Blazor Server
- MudBlazor
- Entity Framework Core 10
- MongoDB (via `MongoDB.EntityFrameworkCore`)
- SQL Server
- Scalar + OpenAPI

## Prerequisites

- .NET 10 SDK
- En MongoDB database
- En SQL Server database

## Configuration

### API (`src/BigData.API/appsettings.json`)

Opret følgende connectionstrings i appsettings.json - men sæt værdien som en secret i projektet.

- `ConnectionStrings:MongoDbConnection`
- `ConnectionStrings:DefaultConnection`

I appsettings.json, kan du sætte kilden til vejr API:

- `DmiWeatherClient:BaseAddress` 

### UI (`src/BigData.UI/appsettings.json`)

Sæt API base URL:

- `WeatherApi:BaseUrl` (default: `https://localhost:7117/`)

## Douglas Leney's V's

- **Volume:** MongoDb (Cloud løsning kan skalere vertikalt og horisontalt mod betaling).
- **Variety:** Forskellige typer dokumenter
- **Velocity:** Kontrollering af hvor meget data vi kan ingeste.
- **Value:** Kan bruges til at forudse storm, 
- **Venue:** Vi har countrycode + stationen med.
- **Veracity:** Kvalitet/integritet af data. 