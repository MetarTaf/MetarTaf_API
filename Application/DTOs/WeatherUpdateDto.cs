namespace Application.DTOs;

/// <summary>
/// DTO der pushes til klienter via SignalR når der er nye vejrdata.
/// </summary>
public sealed record WeatherUpdateDto
{
    public required string Icao { get; init; }
    public MetarDto? NewMetar { get; init; }
    public TafDto? NewTaf { get; init; }
    public required DateTime UpdateTimeUtc { get; init; }
}
