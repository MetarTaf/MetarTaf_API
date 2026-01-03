# MetarTaf API

METAR/TAF vejrovervågnings-API med SignalR push-notifikationer.

## Arkitektur

Projektet følger Hexagonal Architecture (Ports & Adapters):

```
┌─────────────────────────────────────────────────────────────┐
│                         API                                 │
│  Controllers, SignalR Hub                                   │
├─────────────────────────────────────────────────────────────┤
│                      Application                            │
│  Use Cases (WeatherService), DTOs, Interfaces               │
├─────────────────────────────────────────────────────────────┤
│                        Domain                               │
│  Entities (Airport), Value Objects, Ports (interfaces)      │
├─────────────────────────────────────────────────────────────┤
│                     Infrastructure                          │
│  Adapters (NorthAviMet, JsonAirportInfo, Parsers)          │
│  Repositories, Background Services                          │
└─────────────────────────────────────────────────────────────┘
```

## Projekter

- **Domain** - Rene domæne-objekter uden eksterne afhængigheder
- **Application** - Use cases og forretningslogik
- **Infrastructure** - Implementeringer af ports (adapters)
- **Api** - HTTP endpoints og SignalR hub

## API Endpoints

### REST

| Metode | Endpoint | Beskrivelse |
|--------|----------|-------------|
| GET | `/api/airports` | Hent alle overvågede lufthavne |
| GET | `/api/airports/{icao}` | Hent specifik lufthavn |
| POST | `/api/airports/subscriptions/{icao}` | Tilføj lufthavn til overvågning |
| DELETE | `/api/airports/subscriptions/{icao}` | Fjern lufthavn fra overvågning |
| POST | `/api/airports/fetch` | Tving fetch af nye data |

### SignalR Hub

Hub URL: `/hubs/weather`

**Klient → Server metoder:**
- `SubscribeToAirport(icao)` - Modtag updates for én lufthavn
- `UnsubscribeFromAirport(icao)` - Stop updates for én lufthavn  
- `SubscribeToAirports(icaos[])` - Modtag updates for flere lufthavne

**Server → Klient events:**
- `WeatherUpdate` - Sendes når der er nye METAR/TAF data

## Kør projektet

```bash
cd Api
dotnet run
```

API'et starter på `http://localhost:5000`. Swagger UI: `http://localhost:5000/swagger`

## SignalR klient eksempel (JavaScript)

```javascript
import * as signalR from "@microsoft/signalr";

const connection = new signalR.HubConnectionBuilder()
    .withUrl("http://localhost:5000/hubs/weather")
    .withAutomaticReconnect()
    .build();

// Lyt efter opdateringer
connection.on("WeatherUpdate", (update) => {
    console.log(`Ny data for ${update.icao}:`, update);
    if (update.newMetar) {
        console.log("Ny METAR:", update.newMetar.raw);
    }
    if (update.newTaf) {
        console.log("Ny TAF:", update.newTaf.raw);
    }
});

// Start forbindelse og subscribe
await connection.start();
await connection.invoke("SubscribeToAirport", "EKCH");
```

## Konfiguration

I `appsettings.json`:

```json
{
  "FetchIntervalMinutes": 1
}
```

## Data flow

1. Klient kalder `POST /api/airports/subscriptions/EKCH`
2. API opretter Airport-entitet og henter data fra NorthAviMet
3. Klient forbinder til SignalR hub og kalder `SubscribeToAirport("EKCH")`
4. BackgroundService fetcher data hvert minut
5. Når ny METAR/TAF opdages, pushes `WeatherUpdate` til alle subscribers
