using Domain.Entities;

namespace Domain.Ports;

/// <summary>
/// Repository for Airport-entiteter.
/// Holder styr på de lufthavne vi aktivt overvåger.
/// </summary>
public interface IAirportRepository
{
    Task<Airport?> GetAsync(string icao, CancellationToken ct = default);
    Task<IReadOnlyList<Airport>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetAllIcaosAsync(CancellationToken ct = default);
    Task AddAsync(Airport airport, CancellationToken ct = default);
    Task RemoveAsync(string icao, CancellationToken ct = default);
    Task<bool> ExistsAsync(string icao, CancellationToken ct = default);
}
