namespace Domain.ValueObjects;

/// <summary>
/// Immutable value object for en TAF-rapport.
/// </summary>
public sealed record TafData
{
    public required string Icao { get; init; }
    public required DateTime ReportTime { get; init; }
    public required DateTime FetchTime { get; init; }
    public required string RawTaf { get; init; }
    public required TafType Type { get; init; }
    
    public bool IsAmendment => Type == TafType.TafAmd;
}

public enum TafType
{
    Taf,
    TafAmd,
    TafCor
}
