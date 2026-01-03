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

// ---------- CORS (for udvikling) ----------
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
    
    // SignalR kræver credentials, så separat policy
    options.AddPolicy("SignalR", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000", "https://localhost:5001")
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
app.MapHub<WeatherHub>("/hubs/weather").RequireCors("SignalR");

app.MapGet("/healthz", () => Results.Ok("ok"));

app.Run();
