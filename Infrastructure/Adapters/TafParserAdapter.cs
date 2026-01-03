using System.Globalization;
using Application.Interfaces;
using Domain.ValueObjects;
using Taf.Decoder;
using Taf.Decoder.entity;

namespace Infrastructure.Adapters;

/// <summary>
/// Parser adapter for TAF-strenge.
/// Wrapper omkring Taf.Decoder library.
/// </summary>
public sealed class TafParserAdapter : ITafParser
{
    public TafData Parse(string rawTaf)
    {
        // Sørg for at TAF starter med "TAF "
        if (!rawTaf.StartsWith("TAF ", StringComparison.OrdinalIgnoreCase))
            rawTaf = "TAF " + rawTaf;
        
        // Fjern RTD som ikke understøttes
        rawTaf = rawTaf.Replace(" RTD ", " ");
        
        var decoded = TafDecoder.ParseWithMode(rawTaf);
        
        return new TafData
        {
            Icao = decoded.Icao ?? throw new FormatException("Kunne ikke parse ICAO fra TAF"),
            ReportTime = CreateReportTime(decoded.Day ?? 1, decoded.Time ?? "0000 UTC"),
            FetchTime = DateTime.UtcNow,
            RawTaf = decoded.RawTaf ?? rawTaf,
            Type = MapTafType(decoded.Type)
        };
    }

    private static TafType MapTafType(DecodedTaf.TafType type)
    {
        return type switch
        {
            DecodedTaf.TafType.TAF => TafType.Taf,
            DecodedTaf.TafType.TAFAMD => TafType.TafAmd,
            DecodedTaf.TafType.TAFCOR => TafType.TafCor,
            _ => TafType.Taf
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
            var now = DateTime.UtcNow;
            var result = new DateTime(now.Year, now.Month, day, parsedDateTime.Hour, parsedDateTime.Minute, 0, DateTimeKind.Utc);
            
            if (result > now.AddDays(1))
                result = result.AddMonths(-1);
            
            return result;
        }

        return DateTime.UtcNow;
    }
}
