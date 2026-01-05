using Microsoft.AspNetCore.SignalR;
using Application.UseCases;
using System.Collections.Concurrent;

namespace Api.Hubs;

/// <summary>
/// SignalR Hub for vejropdateringer.
/// Klienter kan joine/leave grupper baseret på ICAO-koder.
/// </summary>
public sealed class WeatherHub : Hub
{
    private readonly ILogger<WeatherHub> _logger;
    private readonly WeatherService _weatherService;

    // Track: ConnectionId → Liste af ICAO'er
    private static readonly ConcurrentDictionary<string, HashSet<string>> _connectionSubscriptions = new();

    public WeatherHub(ILogger<WeatherHub> logger, WeatherService weatherService)
    {
        _logger = logger;
        _weatherService = weatherService;
    }

    /// <summary>
    /// Klient joiner en gruppe for at modtage opdateringer for en specifik lufthavn.
    /// </summary>
    public async Task SubscribeToAirport(string icao)
    {
        icao = icao.Trim().ToUpperInvariant();
        await Groups.AddToGroupAsync(Context.ConnectionId, icao);

        // Track subscription
        _connectionSubscriptions.AddOrUpdate(
            Context.ConnectionId,
            _ => new HashSet<string> { icao },
            (_, set) => { lock (set) { set.Add(icao); } return set; }
        );

        _logger.LogInformation("Client {ConnectionId} subscribed to {Icao}", Context.ConnectionId, icao);
    }

    /// <summary>
    /// Klient forlader en gruppe.
    /// </summary>
    public async Task UnsubscribeFromAirport(string icao)
    {
        icao = icao.Trim().ToUpperInvariant();
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, icao);

        // Fjern fra tracking
        if (_connectionSubscriptions.TryGetValue(Context.ConnectionId, out var set))
        {
            lock (set) { set.Remove(icao); }
        }

        // Unsubscribe fra API hvis ingen andre følger denne lufthavn
        if (!AnyoneSubscribedTo(icao))
        {
            await _weatherService.UnsubscribeAsync(icao);
            _logger.LogInformation("No clients left for {Icao} - unsubscribed from API", icao);
        }

        _logger.LogInformation("Client {ConnectionId} unsubscribed from {Icao}", Context.ConnectionId, icao);
    }

    /// <summary>
    /// Klient kan subscribe til flere lufthavne på én gang.
    /// </summary>
    public async Task SubscribeToAirports(IEnumerable<string> icaos)
    {
        var icaoList = icaos.Select(i => i.Trim().ToUpperInvariant()).ToList();

        foreach (var icao in icaoList)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, icao);
        }

        // Track subscriptions
        _connectionSubscriptions.AddOrUpdate(
            Context.ConnectionId,
            _ => new HashSet<string>(icaoList),
            (_, set) => { lock (set) { foreach (var i in icaoList) set.Add(i); } return set; }
        );

        _logger.LogInformation("Client {ConnectionId} subscribed to {Count} airports",
            Context.ConnectionId, icaoList.Count);
    }

    public override Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        return base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);

        // Hent og fjern denne connections lufthavne
        if (_connectionSubscriptions.TryRemove(Context.ConnectionId, out var icaos))
        {
            List<string> icaoList;
            lock (icaos) { icaoList = icaos.ToList(); }

            foreach (var icao in icaoList)
            {
                // Unsubscribe fra API hvis ingen andre følger denne lufthavn
                if (!AnyoneSubscribedTo(icao))
                {
                    await _weatherService.UnsubscribeAsync(icao);
                    _logger.LogInformation("No clients left for {Icao} - unsubscribed from API", icao);
                }
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    private static bool AnyoneSubscribedTo(string icao)
    {
        foreach (var kvp in _connectionSubscriptions)
        {
            lock (kvp.Value)
            {
                if (kvp.Value.Contains(icao))
                    return true;
            }
        }
        return false;
    }
}