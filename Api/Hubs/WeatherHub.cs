using Microsoft.AspNetCore.SignalR;

namespace Api.Hubs;

/// <summary>
/// SignalR Hub for vejropdateringer.
/// Klienter kan joine/leave grupper baseret på ICAO-koder.
/// </summary>
public sealed class WeatherHub : Hub
{
    private readonly ILogger<WeatherHub> _logger;

    public WeatherHub(ILogger<WeatherHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Klient joiner en gruppe for at modtage opdateringer for en specifik lufthavn.
    /// </summary>
    public async Task SubscribeToAirport(string icao)
    {
        icao = icao.Trim().ToUpperInvariant();
        await Groups.AddToGroupAsync(Context.ConnectionId, icao);
        _logger.LogInformation("Client {ConnectionId} subscribed to {Icao}", Context.ConnectionId, icao);
    }

    /// <summary>
    /// Klient forlader en gruppe.
    /// </summary>
    public async Task UnsubscribeFromAirport(string icao)
    {
        icao = icao.Trim().ToUpperInvariant();
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, icao);
        _logger.LogInformation("Client {ConnectionId} unsubscribed from {Icao}", Context.ConnectionId, icao);
    }

    /// <summary>
    /// Klient kan subscribe til flere lufthavne på én gang.
    /// </summary>
    public async Task SubscribeToAirports(IEnumerable<string> icaos)
    {
        foreach (var icao in icaos.Select(i => i.Trim().ToUpperInvariant()))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, icao);
        }
        _logger.LogInformation("Client {ConnectionId} subscribed to {Count} airports", 
            Context.ConnectionId, icaos.Count());
    }

    public override Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }
}
