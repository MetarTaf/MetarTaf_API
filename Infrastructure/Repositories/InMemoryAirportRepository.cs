using System.Collections.Concurrent;
using Domain.Entities;
using Domain.Ports;

namespace Infrastructure.Repositories;

/// <summary>
/// In-memory implementation af IAirportRepository.
/// Thread-safe via ConcurrentDictionary.
/// </summary>
public sealed class InMemoryAirportRepository : IAirportRepository
{
    private readonly ConcurrentDictionary<string, Airport> _airports = new(StringComparer.OrdinalIgnoreCase);

    public Task<Airport?> GetAsync(string icao, CancellationToken ct = default)
    {
        _airports.TryGetValue(icao, out var airport);
        return Task.FromResult(airport);
    }

    public Task<IReadOnlyList<Airport>> GetAllAsync(CancellationToken ct = default)
    {
        IReadOnlyList<Airport> list = _airports.Values.ToList();
        return Task.FromResult(list);
    }

    public Task<IReadOnlyList<string>> GetAllIcaosAsync(CancellationToken ct = default)
    {
        IReadOnlyList<string> list = _airports.Keys.ToList();
        return Task.FromResult(list);
    }

    public Task AddAsync(Airport airport, CancellationToken ct = default)
    {
        _airports.TryAdd(airport.Icao, airport);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string icao, CancellationToken ct = default)
    {
        _airports.TryRemove(icao, out _);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string icao, CancellationToken ct = default)
    {
        return Task.FromResult(_airports.ContainsKey(icao));
    }
}
