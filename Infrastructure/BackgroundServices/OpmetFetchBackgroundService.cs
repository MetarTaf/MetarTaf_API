using Application.UseCases;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.BackgroundServices;

/// <summary>
/// Background service der periodisk henter nye OPMET-data og notificerer klienter.
/// </summary>
public sealed class OpmetFetchBackgroundService : BackgroundService
{
    private readonly WeatherService _weatherService;
    private readonly ILogger<OpmetFetchBackgroundService> _logger;
    private readonly TimeSpan _interval;

    public OpmetFetchBackgroundService(
        WeatherService weatherService,
        ILogger<OpmetFetchBackgroundService> logger,
        TimeSpan? interval = null)
    {
        _weatherService = weatherService;
        _logger = logger;
        _interval = interval ?? TimeSpan.FromMinutes(1);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OpmetFetchBackgroundService started. Interval: {Interval}", _interval);

        // Vent lidt før første fetch så serveren kan starte
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogDebug("Fetching OPMET data...");
                await _weatherService.FetchAndNotifyAsync(stoppingToken);
                _logger.LogDebug("Fetch completed.");
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Graceful shutdown
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during OPMET fetch");
            }

            await Task.Delay(_interval, stoppingToken);
        }

        _logger.LogInformation("OpmetFetchBackgroundService stopped.");
    }
}
