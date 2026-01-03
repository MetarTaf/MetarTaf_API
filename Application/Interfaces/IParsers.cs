using Domain.ValueObjects;

namespace Application.Interfaces;

/// <summary>
/// Parser for METAR-strenge.
/// </summary>
public interface IMetarParser
{
    MetarData Parse(string rawMetar);
}

/// <summary>
/// Parser for TAF-strenge.
/// </summary>
public interface ITafParser
{
    TafData Parse(string rawTaf);
}
