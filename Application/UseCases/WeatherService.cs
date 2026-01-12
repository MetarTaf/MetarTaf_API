using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Ports;
using Domain.ValueObjects;

namespace Application.UseCases;

/// <summary>
/// Application service der koordinerer use cases for vejrovervågning.
/// </summary>
public sealed class WeatherService
{
    private readonly IAirportRepository _airportRepository;
    private readonly IAirportInfoProvider _airportInfoProvider;
    private readonly IOpmetFetcher _opmetFetcher;
    private readonly IWeatherNotifier _notifier;
    private readonly IMetarParser _metarParser;
    private readonly ITafParser _tafParser;

    public WeatherService(
        IAirportRepository airportRepository,
        IAirportInfoProvider airportInfoProvider,
        IOpmetFetcher opmetFetcher,
        IWeatherNotifier notifier,
        IMetarParser metarParser,
        ITafParser tafParser)
    {
        _airportRepository = airportRepository;
        _airportInfoProvider = airportInfoProvider;
        _opmetFetcher = opmetFetcher;
        _notifier = notifier;
        _metarParser = metarParser;
        _tafParser = tafParser;
    }

    /// <summary>
    /// Tilføjer en lufthavn til overvågning.
    /// </summary>
    public async Task<AirportDto?> SubscribeAsync(string icao, CancellationToken ct = default)
    {
        icao = icao.Trim().ToUpperInvariant();

        // Tjek om vi allerede tracker den
        if (await _airportRepository.ExistsAsync(icao, ct))
        {
            var existing = await _airportRepository.GetAsync(icao, ct);
            return existing is null ? null : MapToDto(existing);
        }

        // Hent stamdata
        var info = await _airportInfoProvider.GetByIcaoAsync(icao, ct);
        if (info is null)
            return null;

        // Opret og gem
        var airport = new Airport(info);
        await _airportRepository.AddAsync(airport, ct);

        // Fetch data med det samme
        await FetchAndUpdateSingleAsync(airport, ct);

        return MapToDto(airport);
    }

    /// <summary>
    /// Fjerner en lufthavn fra overvågning.
    /// </summary>
    public async Task<bool> UnsubscribeAsync(string icao, CancellationToken ct = default)
    {
        icao = icao.Trim().ToUpperInvariant();
        
        if (!await _airportRepository.ExistsAsync(icao, ct))
            return false;

        await _airportRepository.RemoveAsync(icao, ct);
        return true;
    }

    /// <summary>
    /// Henter oversigt for en enkelt lufthavn.
    /// </summary>
    public async Task<AirportDto?> GetAirportAsync(string icao, CancellationToken ct = default)
    {
        icao = icao.Trim().ToUpperInvariant();
        var airport = await _airportRepository.GetAsync(icao, ct);
        return airport is null ? null : MapToDto(airport);
    }

    /// <summary>
    /// Henter alle overvågede lufthavne.
    /// </summary>
    public async Task<IReadOnlyList<AirportDto>> GetAllAirportsAsync(CancellationToken ct = default)
    {
        var airports = await _airportRepository.GetAllAsync(ct);
        return airports.Select(MapToDto).ToList();
    }

    /// <summary>
    /// Fetcher nye data fra eksterne kilder og notificerer ved ændringer.
    /// Kaldes typisk af en BackgroundService.
    /// </summary>
    public async Task FetchAndNotifyAsync(CancellationToken ct = default)
    {
        var icaos = await _airportRepository.GetAllIcaosAsync(ct);
        if (icaos.Count == 0)
            return;

        // Fetch fra ekstern kilde
        var (metars, tafs) = await _opmetFetcher.FetchAsync(icaos, ct);

        var updates = new List<WeatherUpdateDto>();

        foreach (var icao in icaos)
        {
            var airport = await _airportRepository.GetAsync(icao, ct);
            if (airport is null) continue;

            MetarDto? newMetar = null;
            TafDto? newTaf = null;

            // Proces METAR
            if (metars.TryGetValue(icao, out var rawMetar))
            {
                try
                {
                    var metarData = _metarParser.Parse(rawMetar);
                    if (airport.AddMetar(metarData))
                    {
                        newMetar = MapMetarToDto(metarData);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Fejl ved parsing af METAR for {icao}: {ex.Message}");
                }
            }

            // Proces TAF
            if (tafs.TryGetValue(icao, out var rawTaf))
            {
                try
                {
                    var tafData = _tafParser.Parse(rawTaf);
                    if (airport.AddTaf(tafData))
                    {
                        newTaf = MapTafToDto(tafData);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Fejl ved parsing af TAF for {icao}: {ex.Message}");
                }
            }

            // Hvis der er noget nyt, tilføj til updates
            if (newMetar is not null || newTaf is not null)
            {
                updates.Add(new WeatherUpdateDto
                {
                    Icao = icao,
                    NewMetar = newMetar,
                    NewTaf = newTaf,
                    UpdateTimeUtc = DateTime.UtcNow
                });
            }
        }

        // Notify alle klienter om opdateringer
        if (updates.Count > 0)
        {
            await _notifier.NotifyManyAsync(updates, ct);
        }
    }

    public async Task AddTestMetarAsync(string icao, MetarData metar)
    {
        var airport = await _airportRepository.GetAsync(icao);
        if (airport == null) return;

        if (airport.AddMetar(metar))
        {
            var dto = new WeatherUpdateDto
            {
                Icao = icao,
                NewMetar = MapMetarToDto(metar),
                UpdateTimeUtc = DateTime.UtcNow
            };
            await _notifier.NotifyAsync(dto);
        }
    }

    public async Task AddTestTafAsync(string icao, TafData taf)
    {
        var airport = await _airportRepository.GetAsync(icao);
        if (airport == null) return;

        if (airport.AddTaf(taf))
        {
            var dto = new WeatherUpdateDto
            {
                Icao = icao,
                NewTaf = MapTafToDto(taf),
                UpdateTimeUtc = DateTime.UtcNow
            };
            await _notifier.NotifyAsync(dto);
        }
    }

    private async Task FetchAndUpdateSingleAsync(Airport airport, CancellationToken ct)
    {
        var (metars, tafs) = await _opmetFetcher.FetchAsync(new[] { airport.Icao }, ct);

        if (metars.TryGetValue(airport.Icao, out var rawMetar))
        {
            try
            {
                var metarData = _metarParser.Parse(rawMetar);
                airport.AddMetar(metarData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{airport.Icao}] METAR parse error: {ex.Message}");
            }
        }

        if (tafs.TryGetValue(airport.Icao, out var rawTaf))
        {
            try
            {
                var tafData = _tafParser.Parse(rawTaf);
                airport.AddTaf(tafData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{airport.Icao}] TAF parse error: {ex.Message}");
            }
        }
    }

    private static AirportDto MapToDto(Airport airport)
    {
        return new AirportDto
        {
            Icao = airport.Icao,
            Country = airport.Info.Country,
            LatestMetar = airport.GetLatestMetar() is { } m ? MapMetarToDto(m) : null,
            LatestTaf = airport.GetLatestTaf() is { } t ? MapTafToDto(t) : null
        };
    }

    private static MetarDto MapMetarToDto(MetarData metar)
    {
        return new MetarDto
        {
            ReportTimeUtc = metar.ReportTime,
            FetchTimeUtc = metar.FetchTime,
            Raw = metar.RawMetar,
            Type = metar.Type.ToString().ToUpperInvariant()
        };
    }

    private static TafDto MapTafToDto(TafData taf)
    {
        return new TafDto
        {
            ReportTimeUtc = taf.ReportTime,
            FetchTimeUtc = taf.FetchTime,
            Raw = taf.RawTaf,
            Type = taf.Type.ToString().ToUpperInvariant(),
            IsAmendment = taf.IsAmendment
        };
    }
}
