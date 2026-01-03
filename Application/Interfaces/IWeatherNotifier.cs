using Application.DTOs;

namespace Application.Interfaces;

/// <summary>
/// Interface for at broadcaste vejropdateringer til klienter.
/// Implementeres af SignalR hub i API-laget.
/// </summary>
public interface IWeatherNotifier
{
    /// <summary>
    /// Sender en vejropdatering til alle klienter der lytter på den givne lufthavn.
    /// </summary>
    Task NotifyAsync(WeatherUpdateDto update, CancellationToken ct = default);
    
    /// <summary>
    /// Sender en vejropdatering til alle klienter der lytter på mindst én af de givne lufthavne.
    /// </summary>
    Task NotifyManyAsync(IEnumerable<WeatherUpdateDto> updates, CancellationToken ct = default);
}
