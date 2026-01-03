namespace Domain.ValueObjects;

/// <summary>
/// Immutable value object for en METAR-rapport.
/// </summary>
public sealed record MetarData
{
    public required string Icao { get; init; }
    public required DateTime ReportTime { get; init; }
    public required DateTime FetchTime { get; init; }
    public required string RawMetar { get; init; }
    public required MetarType Type { get; init; }
}

public enum MetarType
{
    Metar,
    MetarCor,
    Speci,
    SpeciCor
}
