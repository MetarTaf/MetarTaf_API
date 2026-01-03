using Application.DTOs;
using Application.UseCases;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AirportsController : ControllerBase
{
    private readonly WeatherService _weatherService;
    private readonly ILogger<AirportsController> _logger;

    public AirportsController(WeatherService weatherService, ILogger<AirportsController> logger)
    {
        _weatherService = weatherService;
        _logger = logger;
    }

    /// <summary>
    /// Henter alle overvågede lufthavne.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AirportDto>>> GetAll(CancellationToken ct)
    {
        var airports = await _weatherService.GetAllAirportsAsync(ct);
        return Ok(airports);
    }

    /// <summary>
    /// Henter en specifik lufthavn.
    /// </summary>
    [HttpGet("{icao}")]
    public async Task<ActionResult<AirportDto>> Get(string icao, CancellationToken ct)
    {
        var airport = await _weatherService.GetAirportAsync(icao, ct);
        if (airport is null)
            return NotFound();
        
        return Ok(airport);
    }

    /// <summary>
    /// Tilføjer en lufthavn til overvågning (subscribe).
    /// </summary>
    [HttpPost("subscriptions/{icao}")]
    public async Task<ActionResult<AirportDto>> Subscribe(string icao, CancellationToken ct)
    {
        _logger.LogInformation("Subscribe request for {Icao}", icao);
        
        var airport = await _weatherService.SubscribeAsync(icao, ct);
        if (airport is null)
            return NotFound($"Lufthavn med ICAO '{icao}' blev ikke fundet.");
        
        return CreatedAtAction(nameof(Get), new { icao = airport.Icao }, airport);
    }

    /// <summary>
    /// Fjerner en lufthavn fra overvågning (unsubscribe).
    /// </summary>
    [HttpDelete("subscriptions/{icao}")]
    public async Task<IActionResult> Unsubscribe(string icao, CancellationToken ct)
    {
        _logger.LogInformation("Unsubscribe request for {Icao}", icao);
        
        var removed = await _weatherService.UnsubscribeAsync(icao, ct);
        if (!removed)
            return NotFound();
        
        return NoContent();
    }

    /// <summary>
    /// Tvinger en fetch af nye data (til debugging/admin).
    /// </summary>
    [HttpPost("fetch")]
    public async Task<IActionResult> ForceFetch(CancellationToken ct)
    {
        _logger.LogInformation("Force fetch requested");
        await _weatherService.FetchAndNotifyAsync(ct);
        return Ok("Fetch completed");
    }
}
