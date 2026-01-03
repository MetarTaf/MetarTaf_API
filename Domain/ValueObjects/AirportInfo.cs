namespace Domain.ValueObjects;

/// <summary>
/// Immutable value object med stamdata for en lufthavn.
/// </summary>
public sealed record AirportInfo
{
    public string IcaoId { get; init; } = string.Empty;
    public string? IataId { get; init; }
    public string? FaaId { get; init; }
    public string? WmoId { get; init; }
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public int Elevation { get; init; }
    public string? Site { get; init; }
    public string? State { get; init; }
    public string? Country { get; init; }
}
