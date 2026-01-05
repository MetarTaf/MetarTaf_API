using System.Net;
using Api.Hubs;
using Api.Services;
using Application.Interfaces;
using Application.UseCases;
using Domain.Ports;
using Infrastructure.Adapters;
using Infrastructure.BackgroundServices;
using Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// ---------- Konfiguration ----------
var fetchIntervalMinutes = builder.Configuration.GetValue("FetchIntervalMinutes", 1);

// ---------- HTTP Client for NorthAviMet ----------
builder.Services.AddHttpClient<NorthAviMetAdapter>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(20);
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli
});

// ---------- Infrastructure (Adapters & Repositories) ----------
builder.Services.AddSingleton<IOpmetFetcher, NorthAviMetAdapter>();
builder.Services.AddSingleton<IAirportInfoProvider, JsonAirportInfoAdapter>();
builder.Services.AddSingleton<IAirportRepository, InMemoryAirportRepository>();
builder.Services.AddSingleton<IMetarParser, MetarParserAdapter>();
builder.Services.AddSingleton<ITafParser, TafParserAdapter>();

// ---------- Application Services ----------
builder.Services.AddSingleton<WeatherService>();

// ---------- SignalR ----------
builder.Services.AddSignalR();
builder.Services.AddSingleton<IWeatherNotifier, SignalRWeatherNotifier>();

// ---------- Background Service ----------
builder.Services.AddHostedService(sp => new OpmetFetchBackgroundService(
    sp.GetRequiredService<WeatherService>(),
    sp.GetRequiredService<ILogger<OpmetFetchBackgroundService>>(),
    TimeSpan.FromMinutes(fetchIntervalMinutes)
));

// ---------- Controllers ----------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "MetarTaf API", Version = "v1" });
});

// ---------- CORS ----------
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
            // Udvikling (din lokale PC)
            "http://localhost:60600",
            "http://192.168.1.153:60600",

            // Pre frontend (når den hostes på serveren)
            "http://192.168.1.162:5003",

            // Offentlige domæner
            "https://pre.metartaf.cbmprojects.dk",
            "https://metartaf.cbmprojects.dk",

            // Tilføj flere efter behov
            "http://localhost:5173",
            "http://localhost:5200"
        )
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials();
    });
});

var app = builder.Build();

// ---------- Initialiser airport data ----------
using (var scope = app.Services.CreateScope())
{
    var airportInfoProvider = scope.ServiceProvider.GetRequiredService<IAirportInfoProvider>();
    if (airportInfoProvider is JsonAirportInfoAdapter adapter)
    {
        await adapter.EnsureInitializedAsync();
    }
    
    // Tilføj TEST-lufthavn som default
    var weatherService = scope.ServiceProvider.GetRequiredService<WeatherService>();
    await weatherService.SubscribeAsync("TEST");
}

// ---------- Middleware Pipeline ----------
    app.UseSwagger();
    app.UseSwaggerUI();

app.UseCors();

app.UseRouting();

app.MapControllers();
app.MapHub<WeatherHub>("/hubs/weather");

app.MapGet("/healthz", () => Results.Ok("ok"));

app.Run();
