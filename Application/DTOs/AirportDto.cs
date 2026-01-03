namespace Application.DTOs;

public sealed record AirportDto
{
    public required string Icao { get; init; }
    public string? Country { get; init; }
    public MetarDto? LatestMetar { get; init; }
    public TafDto? LatestTaf { get; init; }
}

public sealed record MetarDto
{
    public required DateTime ReportTimeUtc { get; init; }
    public required DateTime FetchTimeUtc { get; init; }
    public required string Raw { get; init; }
    public required string Type { get; init; }
}

public sealed record TafDto
{
    public required DateTime ReportTimeUtc { get; init; }
    public required DateTime FetchTimeUtc { get; init; }
    public required string Raw { get; init; }
    public required string Type { get; init; }
    public required bool IsAmendment { get; init; }
}
