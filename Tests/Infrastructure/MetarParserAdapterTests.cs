using Domain.ValueObjects;
using FluentAssertions;
using Infrastructure.Adapters;
using Xunit;

namespace Tests.Infrastructure;

public class MetarParserAdapterTests
{
    private readonly MetarParserAdapter _sut = new();

    #region Valid METAR Parsing

    [Fact]
    public void Parse_ValidMetar_ReturnsCorrectIcao()
    {
        var rawMetar = "METAR EKCH 091920Z 17008KT 9999 BKN190 18/15 Q1018";

        var result = _sut.Parse(rawMetar);

        result.Icao.Should().Be("EKCH");
    }

    [Fact]
    public void Parse_ValidMetar_ReturnsRawMetar()
    {
        var rawMetar = "METAR EKCH 091920Z 17008KT 9999 BKN190 18/15 Q1018";

        var result = _sut.Parse(rawMetar);

        result.RawMetar.Should().Contain("EKCH");
    }

    [Fact]
    public void Parse_ValidMetar_SetsReportTime()
    {
        var rawMetar = "METAR EKCH 091920Z 17008KT 9999 BKN190 18/15 Q1018";

        var result = _sut.Parse(rawMetar);

        result.ReportTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromDays(32));
        result.ReportTime.Day.Should().Be(9);
        result.ReportTime.Hour.Should().Be(19);
        result.ReportTime.Minute.Should().Be(20);
    }

    [Fact]
    public void Parse_ValidMetar_SetsFetchTime()
    {
        var rawMetar = "METAR EKCH 091920Z 17008KT 9999 BKN190 18/15 Q1018";

        var result = _sut.Parse(rawMetar);

        result.FetchTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    #endregion

    #region METAR Type Mapping

    [Fact]
    public void Parse_MetarType_ReturnsMetar()
    {
        var rawMetar = "METAR EKCH 091920Z 17008KT 9999 BKN190 18/15 Q1018";

        var result = _sut.Parse(rawMetar);

        result.Type.Should().Be(MetarType.Metar);
    }

    [Fact]
    public void Parse_MetarCorType_ReturnsMetarCor()
    {
        var rawMetar = "METAR COR EKCH 091921Z 17010KT 9999 FEW020 18/15 Q1018";

        var result = _sut.Parse(rawMetar);

        result.Type.Should().Be(MetarType.MetarCor);
    }

    [Fact]
    public void Parse_SpeciType_ReturnsSpeci()
    {
        var rawMetar = "SPECI EKCH 091925Z 18012KT 6000 SHRA SCT018CB 18/14 Q1017";

        var result = _sut.Parse(rawMetar);

        result.Type.Should().Be(MetarType.Speci);
    }

    [Fact]
    public void Parse_SpeciCorType_ReturnsSpeciCor()
    {
        var rawMetar = "SPECI COR EKCH 091926Z 18012KT 6000 SHRA SCT018CB 18/14 Q1017";

        var result = _sut.Parse(rawMetar);

        result.Type.Should().Be(MetarType.SpeciCor);
    }

    #endregion

    #region AUTO Keyword Handling

    [Fact]
    public void Parse_MetarWithAuto_RemovesAutoKeyword()
    {
        var rawMetar = "METAR EKCH 091920Z AUTO 17008KT 9999 BKN190 18/15 Q1018";

        var result = _sut.Parse(rawMetar);

        result.Icao.Should().Be("EKCH");
    }

    #endregion

    #region Date/Time Edge Cases

    [Fact]
    public void Parse_DayInPast_ReturnsCorrectDate()
    {
        // Dag 01 - tidlig i måneden
        var rawMetar = "METAR EKCH 010000Z 17008KT 9999 BKN190 18/15 Q1018";

        var result = _sut.Parse(rawMetar);

        result.ReportTime.Day.Should().Be(1);
    }

    [Fact]
    public void Parse_Day31_ReturnsCorrectDate()
    {
        // Dag 31 - sent i måneden
        var rawMetar = "METAR EKCH 311200Z 17008KT 9999 BKN190 18/15 Q1018";

        var result = _sut.Parse(rawMetar);

        result.ReportTime.Day.Should().Be(31);
    }

    #endregion

    #region Different Airport ICAO Codes

    [Theory]
    [InlineData("METAR KJFK 091920Z 17008KT 9999 BKN190 18/15 Q1018", "KJFK")]
    [InlineData("METAR EGLL 091920Z 17008KT 9999 BKN190 18/15 Q1018", "EGLL")]
    [InlineData("METAR LFPG 091920Z 17008KT 9999 BKN190 18/15 Q1018", "LFPG")]
    [InlineData("METAR LEMD 091920Z 17008KT 9999 BKN190 18/15 Q1018", "LEMD")]
    public void Parse_DifferentIcaoCodes_ParsesCorrectly(string rawMetar, string expectedIcao)
    {
        var result = _sut.Parse(rawMetar);

        result.Icao.Should().Be(expectedIcao);
    }

    #endregion

    #region Complex METAR Content

    [Fact]
    public void Parse_MetarWithNosig_ParsesCorrectly()
    {
        var rawMetar = "METAR EKCH 091920Z 17008KT 9999 BKN190 18/15 Q1018 NOSIG";

        var result = _sut.Parse(rawMetar);

        result.Icao.Should().Be("EKCH");
        result.Type.Should().Be(MetarType.Metar);
    }

    [Fact]
    public void Parse_MetarWithTempo_ParsesCorrectly()
    {
        var rawMetar = "METAR EKCH 091920Z 17008KT 9999 BKN190 18/15 Q1018 TEMPO 2000 TSRA";

        var result = _sut.Parse(rawMetar);

        result.Icao.Should().Be("EKCH");
    }

    [Fact]
    public void Parse_MetarWithCavok_ParsesCorrectly()
    {
        var rawMetar = "METAR EKCH 091920Z 17008KT CAVOK 18/15 Q1018";

        var result = _sut.Parse(rawMetar);

        result.Icao.Should().Be("EKCH");
    }

    [Fact]
    public void Parse_MetarWithRvr_ParsesCorrectly()
    {
        var rawMetar = "METAR EKCH 091920Z 17008KT 0500 R22L/0800 FG VV002 10/10 Q1018";

        var result = _sut.Parse(rawMetar);

        result.Icao.Should().Be("EKCH");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Parse_MinimalValidMetar_ParsesSuccessfully()
    {
        // Minimal METAR with required fields
        var rawMetar = "METAR EKCH 091920Z 17008KT 9999 18/15 Q1018";

        var result = _sut.Parse(rawMetar);

        result.Icao.Should().Be("EKCH");
    }

    [Fact]
    public void Parse_MetarWithSlashes_ParsesCorrectly()
    {
        // METAR with cloud layer with missing temperature indicator (///)
        var rawMetar = "METAR EKCH 091920Z 17008KT 9999 BKN190/// 18/15 Q1018";

        var result = _sut.Parse(rawMetar);

        result.Icao.Should().Be("EKCH");
    }

    #endregion
}
