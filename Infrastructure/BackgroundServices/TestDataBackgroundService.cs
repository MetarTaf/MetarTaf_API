using Application.UseCases;
using Domain.ValueObjects;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.BackgroundServices;

public class TestDataBackgroundService : BackgroundService
{
    private readonly WeatherService _weatherService;
    private readonly ILogger<TestDataBackgroundService> _logger;
    private readonly Random _random = new();

    private static readonly string[] MetarTypes = { "METAR", "METAR", "METAR", "SPECI", "METAR COR", "SPECI COR" };
    private static readonly string[] TafTypes = { "TAF", "TAF", "TAF AMD", "TAF COR" };
    private static readonly string[] WindDirs = { "270", "280", "290", "300", "310", "320", "VRB" };
    private static readonly string[] Clouds = { "CAVOK", "FEW020", "SCT030", "BKN040", "OVC050", "FEW015CB", "BKN025TCU" };
    private static readonly string[] Weather = { "", "", "", "RA", "-RA", "+RA", "SHRA", "TSRA", "SN", "-SN", "BR", "FG" };

    public TestDataBackgroundService(
        WeatherService weatherService,
        ILogger<TestDataBackgroundService> logger)
    {
        _weatherService = weatherService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("TestDataBackgroundService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            // Vent 15-45 sekunder mellem opdateringer
            var delay = TimeSpan.FromSeconds(_random.Next(15, 45));
            await Task.Delay(delay, stoppingToken);

            try
            {
                await GenerateTestDataAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating test data");
            }
        }
    }

    private async Task GenerateTestDataAsync()
    {
        var now = DateTime.UtcNow;
        var ddHHmm = now.ToString("ddHHmm");

        // Generer METAR (altid)
        var metarType = MetarTypes[_random.Next(MetarTypes.Length)];
        var isSpeci = metarType.Contains("SPECI");
        var isCor = metarType.Contains("COR");

        var wind = $"{WindDirs[_random.Next(WindDirs.Length)]}{_random.Next(5, 25):D2}KT";
        var vis = _random.Next(10) > 2 ? "9999" : $"{_random.Next(1000, 9000)}";
        var wx = Weather[_random.Next(Weather.Length)];
        var cloud = Clouds[_random.Next(Clouds.Length)];
        var temp = $"{_random.Next(-10, 30):D2}/{_random.Next(-15, 25):D2}";
        var qnh = $"Q{_random.Next(990, 1035)}";

        var rawMetar = $"{(isSpeci ? "SPECI" : "METAR")}{(isCor ? " COR" : "")} TEST {ddHHmm}Z {wind} {vis} {wx} {cloud} {temp} {qnh}".Replace("  ", " ");

        var metar = new MetarData
        {
            Icao = "TEST",
            ReportTime = now,
            FetchTime = now,
            RawMetar = rawMetar,
            Type = isSpeci ? MetarType.Speci : MetarType.Metar
        };

        await _weatherService.AddTestMetarAsync("TEST", metar);
        _logger.LogDebug("Generated TEST METAR: {Type}", metarType);

        // Generer TAF (ca. hver 3. gang)
        if (_random.Next(3) == 0)
        {
            var tafType = TafTypes[_random.Next(TafTypes.Length)];
            var isAmd = tafType.Contains("AMD");
            var isTafCor = tafType.Contains("COR");

            var validFrom = now.ToString("ddHH");
            var validTo = now.AddHours(24).ToString("ddHH");

            var rawTaf = $"TAF{(isAmd ? " AMD" : "")}{(isTafCor ? " COR" : "")} TEST {ddHHmm}Z {validFrom}/{validTo} {wind} {vis} {cloud}";

            var taf = new TafData
            {
                Icao = "TEST",
                ReportTime = now,
                FetchTime = now,
                RawTaf = rawTaf,
                Type = isAmd ? TafType.TafAmd : (isTafCor ? TafType.TafCor : TafType.Taf)
            };

            await _weatherService.AddTestTafAsync("TEST", taf);
            _logger.LogDebug("Generated TEST TAF: {Type}", tafType);
        }
    }
}