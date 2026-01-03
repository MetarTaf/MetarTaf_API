using System.Collections.Concurrent;
using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
using Domain.Ports;
using Domain.ValueObjects;

namespace Infrastructure.Adapters;

/// <summary>
/// Adapter der henter lufthavnsinfo fra en JSON-fil (cached fra aviationweather.gov).
/// </summary>
public sealed class JsonAirportInfoAdapter : IAirportInfoProvider
{
    private readonly ConcurrentDictionary<string, AirportInfo> _cache = new(StringComparer.OrdinalIgnoreCase);
    private bool _initialized;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    private static readonly string InfoDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Info");
    private static readonly string GzipFilePath = Path.Combine(InfoDirectory, "airports.json.gz");
    private static readonly string JsonFilePath = Path.Combine(InfoDirectory, "airports.json");

    public async Task<AirportInfo?> GetByIcaoAsync(string icao, CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct);
        _cache.TryGetValue(icao.ToUpperInvariant(), out var info);
        return info;
    }

    public async Task EnsureInitializedAsync(CancellationToken ct = default)
    {
        if (_initialized) return;

        await _initLock.WaitAsync(ct);
        try
        {
            if (_initialized) return;

            Directory.CreateDirectory(InfoDirectory);

            // Download hvis filen ikke findes
            if (!File.Exists(JsonFilePath))
            {
                await DownloadAndExtractAsync(ct);
            }

            // Læs og parse
            var json = await File.ReadAllTextAsync(JsonFilePath, ct);
            var entries = JsonSerializer.Deserialize<List<JsonAirportEntry>>(json) ?? [];

            foreach (var entry in entries.Where(e => !string.IsNullOrEmpty(e.IcaoId)))
            {
                var info = new AirportInfo
                {
                    IcaoId = entry.IcaoId!,
                    IataId = entry.IataId,
                    FaaId = entry.FaaId,
                    WmoId = entry.WmoId,
                    Latitude = entry.Lat ?? 0,
                    Longitude = entry.Lon ?? 0,
                    Elevation = entry.Elev ?? 0,
                    Site = entry.Site,
                    State = entry.State,
                    Country = entry.Country
                };
                _cache.TryAdd(info.IcaoId, info);
            }

            // Tilføj TEST-lufthavn
            _cache.TryAdd("TEST", new AirportInfo
            {
                IcaoId = "TEST",
                Country = "Test Country",
                Latitude = 0,
                Longitude = 0,
                Elevation = 0
            });

            _initialized = true;
            Console.WriteLine($"Loaded {_cache.Count} airports.");
        }
        finally
        {
            _initLock.Release();
        }
    }

    private static async Task DownloadAndExtractAsync(CancellationToken ct)
    {
        const string url = "https://aviationweather.gov/data/cache/stations.cache.json.gz";

        Console.WriteLine("Downloading airport data...");

        using var http = new HttpClient();
        var response = await http.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();

        await using (var fs = new FileStream(GzipFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await response.Content.CopyToAsync(fs, ct);
        }

        Console.WriteLine("Extracting...");

        await using var gzipStream = new GZipStream(
            new FileStream(GzipFilePath, FileMode.Open), 
            CompressionMode.Decompress);
        await using var outputStream = new FileStream(JsonFilePath, FileMode.Create);
        await gzipStream.CopyToAsync(outputStream, ct);

        Console.WriteLine("Airport data ready.");
    }

    private sealed class JsonAirportEntry
    {
        [JsonPropertyName("icaoId")]
        public string? IcaoId { get; set; }
        
        [JsonPropertyName("iataId")]
        public string? IataId { get; set; }
        
        [JsonPropertyName("faaId")]
        public string? FaaId { get; set; }
        
        [JsonPropertyName("wmoId")]
        public string? WmoId { get; set; }
        
        [JsonPropertyName("lat")]
        public double? Lat { get; set; }
        
        [JsonPropertyName("lon")]
        public double? Lon { get; set; }
        
        [JsonPropertyName("elev")]
        public int? Elev { get; set; }
        
        [JsonPropertyName("site")]
        public string? Site { get; set; }
        
        [JsonPropertyName("state")]
        public string? State { get; set; }
        
        [JsonPropertyName("country")]
        public string? Country { get; set; }
    }
}
