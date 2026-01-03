using Domain.ValueObjects;

namespace Domain.Ports;

/// <summary>
/// Port for at hente stamdata om lufthavne.
/// Implementeres af infrastruktur-adapter.
/// </summary>
public interface IAirportInfoProvider
{
    Task<AirportInfo?> GetByIcaoAsync(string icao, CancellationToken ct = default);
}
