using Api.Hubs;
using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace Api.Services;

/// <summary>
/// Implementation af IWeatherNotifier der broadcaster via SignalR.
/// </summary>
public sealed class SignalRWeatherNotifier : IWeatherNotifier
{
    private readonly IHubContext<WeatherHub> _hubContext;
    private readonly ILogger<SignalRWeatherNotifier> _logger;

    public SignalRWeatherNotifier(
        IHubContext<WeatherHub> hubContext, 
        ILogger<SignalRWeatherNotifier> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task NotifyAsync(WeatherUpdateDto update, CancellationToken ct = default)
    {
        _logger.LogInformation("Broadcasting update for {Icao}", update.Icao);
        
        // Send til gruppen med ICAO som navn
        await _hubContext.Clients
            .Group(update.Icao)
            .SendAsync("WeatherUpdate", update, ct);
    }

    public async Task NotifyManyAsync(IEnumerable<WeatherUpdateDto> updates, CancellationToken ct = default)
    {
        foreach (var update in updates)
        {
            await NotifyAsync(update, ct);
        }
    }
}
