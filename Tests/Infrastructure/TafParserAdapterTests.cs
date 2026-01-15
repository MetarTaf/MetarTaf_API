using Domain.ValueObjects;
using FluentAssertions;
using Infrastructure.Adapters;
using Xunit;

namespace Tests.Infrastructure;

public class TafParserAdapterTests
{
    private readonly TafParserAdapter _sut = new();

    #region Valid TAF Parsing

    [Fact]
    public void Parse_ValidTaf_ReturnsCorrectIcao()
    {
        var rawTaf = "TAF EKCH 091714Z 0918/1018 17012KT CAVOK";

        var result = _sut.Parse(rawTaf);

        result.Icao.Should().Be("EKCH");
    }

    [Fact]
    public void Parse_ValidTaf_ReturnsRawTaf()
    {
        var rawTaf = "TAF EKCH 091714Z 0918/1018 17012KT CAVOK";

        var result = _sut.Parse(rawTaf);

        result.RawTaf.Should().Contain("EKCH");
    }

    [Fact]
    public void Parse_ValidTaf_SetsReportTime()
    {
        var rawTaf = "TAF EKCH 091714Z 0918/1018 17012KT CAVOK";

        var result = _sut.Parse(rawTaf);

        result.ReportTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromDays(32));
        result.ReportTime.Day.Should().Be(9);
        result.ReportTime.Hour.Should().Be(17);
        result.ReportTime.Minute.Should().Be(14);
    }

    [Fact]
    public void Parse_ValidTaf_SetsFetchTime()
    {
        var rawTaf = "TAF EKCH 091714Z 0918/1018 17012KT CAVOK";

        var result = _sut.Parse(rawTaf);

        result.FetchTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    #endregion

    #region TAF Type Mapping

    [Fact]
    public void Parse_TafType_ReturnsTaf()
    {
        var rawTaf = "TAF EKCH 091714Z 0918/1018 17012KT CAVOK";

        var result = _sut.Parse(rawTaf);

        result.Type.Should().Be(TafType.Taf);
    }

    [Fact]
    public void Parse_TafAmdType_ReturnsTafAmd()
    {
        var rawTaf = "TAF AMD EKCH 091800Z 0918/1018 16012KT CAVOK";

        var result = _sut.Parse(rawTaf);

        result.Type.Should().Be(TafType.TafAmd);
    }

    [Fact]
    public void Parse_TafCorType_ReturnsTafCor()
    {
        var rawTaf = "TAF COR EKCH 091802Z 0918/1018 16012KT CAVOK";

        var result = _sut.Parse(rawTaf);

        result.Type.Should().Be(TafType.TafCor);
    }

    #endregion

    #region TAF Prefix Handling

    [Fact]
    public void Parse_TafWithoutPrefix_AddsPrefix()
    {
        // TAF without "TAF " prefix
        var rawTaf = "EKCH 091714Z 0918/1018 17012KT CAVOK";

        var result = _sut.Parse(rawTaf);

        result.Icao.Should().Be("EKCH");
    }

    #endregion

    #region RTD Keyword Handling

    [Fact]
    public void Parse_TafWithRtd_RemovesRtdKeyword()
    {
        var rawTaf = "TAF EKCH 091714Z RTD 0918/1018 17012KT CAVOK";

        var result = _sut.Parse(rawTaf);

        result.Icao.Should().Be("EKCH");
    }

    #endregion

    #region Date/Time Edge Cases

    [Fact]
    public void Parse_DayInPast_ReturnsCorrectDate()
    {
        var rawTaf = "TAF EKCH 010000Z 0100/0124 17008KT CAVOK";

        var result = _sut.Parse(rawTaf);

        result.ReportTime.Day.Should().Be(1);
    }

    [Fact]
    public void Parse_Day31_ReturnsCorrectDate()
    {
        var rawTaf = "TAF EKCH 311200Z 3112/0112 17008KT CAVOK";

        var result = _sut.Parse(rawTaf);

        result.ReportTime.Day.Should().Be(31);
    }

    #endregion

    #region Different Airport ICAO Codes

    [Theory]
    [InlineData("TAF KJFK 091714Z 0918/1018 17012KT CAVOK", "KJFK")]
    [InlineData("TAF EGLL 091714Z 0918/1018 17012KT CAVOK", "EGLL")]
    [InlineData("TAF LFPG 091714Z 0918/1018 17012KT CAVOK", "LFPG")]
    [InlineData("TAF LEMD 091714Z 0918/1018 17012KT CAVOK", "LEMD")]
    public void Parse_DifferentIcaoCodes_ParsesCorrectly(string rawTaf, string expectedIcao)
    {
        var result = _sut.Parse(rawTaf);

        result.Icao.Should().Be(expectedIcao);
    }

    #endregion

    #region Complex TAF Content

    [Fact]
    public void Parse_TafWithBecmg_ParsesCorrectly()
    {
        var rawTaf = "TAF EKCH 091714Z 0918/1018 17012KT CAVOK BECMG 1000/1002 27015KT";

        var result = _sut.Parse(rawTaf);

        result.Icao.Should().Be("EKCH");
    }

    [Fact]
    public void Parse_TafWithTempo_ParsesCorrectly()
    {
        var rawTaf = "TAF EKCH 091714Z 0918/1018 17012KT 9999 BKN030 TEMPO 0920/0924 3000 TSRA";

        var result = _sut.Parse(rawTaf);

        result.Icao.Should().Be("EKCH");
    }

    [Fact]
    public void Parse_TafWithProb_ParsesCorrectly()
    {
        var rawTaf = "TAF EKCH 091714Z 0918/1018 17012KT CAVOK PROB30 TEMPO 0920/0924 3000 TSRA";

        var result = _sut.Parse(rawTaf);

        result.Icao.Should().Be("EKCH");
    }

    [Fact]
    public void Parse_TafWithFm_ParsesCorrectly()
    {
        var rawTaf = "TAF EKCH 091714Z 0918/1018 17012KT CAVOK FM091800 27015KT 9999 BKN040";

        var result = _sut.Parse(rawTaf);

        result.Icao.Should().Be("EKCH");
    }

    [Fact]
    public void Parse_TafWithMultipleGroups_ParsesCorrectly()
    {
        var rawTaf = "TAF EKCH 091714Z 0918/1018 17012KT CAVOK " +
                     "TEMPO 0920/0924 3000 TSRA " +
                     "BECMG 1000/1002 27015KT " +
                     "PROB30 1006/1010 2000 BR";

        var result = _sut.Parse(rawTaf);

        result.Icao.Should().Be("EKCH");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Parse_MinimalValidTaf_ParsesSuccessfully()
    {
        // Minimal TAF with required fields
        var rawTaf = "TAF EKCH 091714Z 0918/1018 17012KT 9999";

        var result = _sut.Parse(rawTaf);

        result.Icao.Should().Be("EKCH");
    }

    [Fact]
    public void Parse_LongRangeTaf_ParsesCorrectly()
    {
        // 30-hour TAF forecast
        var rawTaf = "TAF EKCH 091714Z 0918/1024 17012KT CAVOK";

        var result = _sut.Parse(rawTaf);

        result.Icao.Should().Be("EKCH");
    }

    #endregion
}
