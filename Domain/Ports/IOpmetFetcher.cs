namespace Domain.Ports;

/// <summary>
/// Port for at hente rå OPMET-data (METAR/TAF) fra eksterne kilder.
/// Implementeres af infrastruktur-adapter (fx NorthAviMet).
/// </summary>
public interface IOpmetFetcher
{
    /// <summary>
    /// Henter seneste METAR og TAF for en liste af ICAO-koder.
    /// </summary>
    /// <returns>Tuple med dictionaries: (ICAO → rå METAR-streng, ICAO → rå TAF-streng)</returns>
    Task<(Dictionary<string, string> Metars, Dictionary<string, string> Tafs)> 
        FetchAsync(IEnumerable<string> icaos, CancellationToken ct = default);
}
