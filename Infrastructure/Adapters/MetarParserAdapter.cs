using System.Globalization;
using Application.Interfaces;
using Domain.ValueObjects;
using Metar.Decoder;
using Metar.Decoder.Entity;

namespace Infrastructure.Adapters;

/// <summary>
/// Parser adapter for METAR-strenge. 
/// Wrapper omkring Metar.Decoder library.
/// </summary>
public sealed class MetarParserAdapter : IMetarParser
{
    public MetarData Parse(string rawMetar)
    {
        // Fjern AUTO som ikke understøttes godt
        rawMetar = rawMetar.Replace(" AUTO ", " ");
        
        var decoded = MetarDecoder.ParseWithMode(rawMetar);
        
        return new MetarData
        {
            Icao = decoded.ICAO ?? throw new FormatException("Kunne ikke parse ICAO fra METAR"),
            ReportTime = CreateReportTime(decoded.Day ?? 1, decoded.Time ?? "0000 UTC"),
            FetchTime = DateTime.UtcNow,
            RawMetar = decoded.RawMetar ?? rawMetar,
            Type = MapMetarType(decoded.Type)
        };
    }

    private static MetarType MapMetarType(DecodedMetar.MetarType type)
    {
        return type switch
        {
            DecodedMetar.MetarType.METAR => MetarType.Metar,
            DecodedMetar.MetarType.METAR_COR => MetarType.MetarCor,
            DecodedMetar.MetarType.SPECI => MetarType.Speci,
            DecodedMetar.MetarType.SPECI_COR => MetarType.SpeciCor,
            _ => MetarType.Metar
        };
    }

    private static DateTime CreateReportTime(int day, string time)
    {
        var dayStr = day <= 9 ? "0" + day : day.ToString();
        var dateTimeString = dayStr + time;
        const string format = "ddHH:mm 'UTC'";

        if (DateTime.TryParseExact(dateTimeString, format, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsedDateTime))
        {
            // Juster år og måned baseret på nuværende dato
            var now = DateTime.UtcNow;
            var result = new DateTime(now.Year, now.Month, day, parsedDateTime.Hour, parsedDateTime.Minute, 0, DateTimeKind.Utc);
            
            // Hvis dagen er i fremtiden, antag forrige måned
            if (result > now.AddDays(1))
                result = result.AddMonths(-1);
            
            return result;
        }

        return DateTime.UtcNow;
    }
}
