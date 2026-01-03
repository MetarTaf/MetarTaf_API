using Domain.ValueObjects;

namespace Domain.Entities;

/// <summary>
/// Aggregate root for en lufthavn med tilhørende vejrdata.
/// Ren domæneentitet uden infrastruktur-afhængigheder.
/// </summary>
public sealed class Airport
{
    private readonly Dictionary<DateTime, MetarData> _metars = new();
    private readonly Dictionary<DateTime, TafData> _tafs = new();
    private readonly object _lock = new();

    public AirportInfo Info { get; }
    public string Icao => Info.IcaoId;

    public Airport(AirportInfo info)
    {
        Info = info ?? throw new ArgumentNullException(nameof(info));
    }

    /// <summary>
    /// Tilføjer en METAR hvis den ikke allerede findes.
    /// </summary>
    /// <returns>True hvis METAR blev tilføjet (ny), false hvis den allerede fandtes.</returns>
    public bool AddMetar(MetarData metar)
    {
        if (metar.Icao != Icao)
            throw new ArgumentException($"METAR ICAO {metar.Icao} matcher ikke lufthavn {Icao}");

        lock (_lock)
        {
            return _metars.TryAdd(metar.ReportTime, metar);
        }
    }

    /// <summary>
    /// Tilføjer en TAF hvis den ikke allerede findes.
    /// </summary>
    /// <returns>True hvis TAF blev tilføjet (ny), false hvis den allerede fandtes.</returns>
    public bool AddTaf(TafData taf)
    {
        if (taf.Icao != Icao)
            throw new ArgumentException($"TAF ICAO {taf.Icao} matcher ikke lufthavn {Icao}");

        lock (_lock)
        {
            return _tafs.TryAdd(taf.ReportTime, taf);
        }
    }

    public MetarData? GetLatestMetar()
    {
        lock (_lock)
        {
            return _metars.Count == 0 
                ? null 
                : _metars.OrderByDescending(kv => kv.Key).First().Value;
        }
    }

    public TafData? GetLatestTaf()
    {
        lock (_lock)
        {
            return _tafs.Count == 0 
                ? null 
                : _tafs.OrderByDescending(kv => kv.Key).First().Value;
        }
    }

    public IReadOnlyList<MetarData> GetAllMetars()
    {
        lock (_lock)
        {
            return _metars.Values.OrderByDescending(m => m.ReportTime).ToList();
        }
    }

    public IReadOnlyList<TafData> GetAllTafs()
    {
        lock (_lock)
        {
            return _tafs.Values.OrderByDescending(t => t.ReportTime).ToList();
        }
    }
}
