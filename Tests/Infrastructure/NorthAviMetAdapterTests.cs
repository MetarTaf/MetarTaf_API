using FluentAssertions;
using Infrastructure.Adapters;
using Xunit;

namespace Tests.Infrastructure;

public class NorthAviMetAdapterTests
{
    private static HttpClient ClientFor(string htmlOrText)
        => new(new FakeHandler(htmlOrText));

    // HTML helper – én række (TAF/METAR/SPECI, optional modifier i 1. celle)
    private static string Row(string firstCell, string icao, string body)
        => $"<table><tr>" +
           $"<td><b>{firstCell}</b></td>" +
           $"<td><b>{icao}</b></td>" +
           $"<td><b> {body}=</b></td>" +
           $"</tr></table>";

    [Fact]
    public async Task Parses_METAR()
    {
        var html = Row("METAR", "EKCH", "091920Z 17008KT 9999 BKN190/// 18/15 Q1018 NOSIG");
        var fetcher = new NorthAviMetAdapter(ClientFor(html));

        var (metar, taf) = await fetcher.FetchAsync(new[] { "EKCH" });
        
        metar["EKCH"].Should().Be("METAR EKCH 091920Z 17008KT 9999 BKN190/// 18/15 Q1018 NOSIG");
        taf.Should().BeEmpty();
    }

    [Fact]
    public async Task Parses_METAR_COR()
    {
        var html = Row("METAR COR", "EKCH", "091921Z 17010KT 9999 FEW020 18/15 Q1018");
        var fetcher = new NorthAviMetAdapter(ClientFor(html));

        var (metar, _) = await fetcher.FetchAsync(new[] { "EKCH" });
        
        metar["EKCH"].Should().Be("METAR COR EKCH 091921Z 17010KT 9999 FEW020 18/15 Q1018");
    }

    [Fact]
    public async Task Parses_SPECI_as_METAR()
    {
        var html = Row("SPECI", "EKCH", "091925Z 18012KT 6000 SHRA SCT018CB 18/14 Q1017");
        var fetcher = new NorthAviMetAdapter(ClientFor(html));

        var (metar, _) = await fetcher.FetchAsync(new[] { "EKCH" });
        
        metar["EKCH"].Should().Be("METAR EKCH 091925Z 18012KT 6000 SHRA SCT018CB 18/14 Q1017");
    }

    [Fact]
    public async Task Parses_SPECI_COR_as_METAR_COR()
    {
        var html = Row("SPECI COR", "EKCH", "091926Z 18012KT 6000 SHRA SCT018CB 18/14 Q1017");
        var fetcher = new NorthAviMetAdapter(ClientFor(html));

        var (metar, _) = await fetcher.FetchAsync(new[] { "EKCH" });
        
        metar["EKCH"].Should().Be("METAR COR EKCH 091926Z 18012KT 6000 SHRA SCT018CB 18/14 Q1017");
    }

    [Fact]
    public async Task Parses_TAF()
    {
        var html = Row("TAF", "EKCH", "091714Z 0918/1018 17012KT CAVOK");
        var fetcher = new NorthAviMetAdapter(ClientFor(html));

        var (_, taf) = await fetcher.FetchAsync(new[] { "EKCH" });
        
        taf["EKCH"].Should().Be("TAF EKCH 091714Z 0918/1018 17012KT CAVOK");
    }

    [Fact]
    public async Task Parses_TAF_AMD_in_first_cell()
    {
        var html = Row("TAF AMD", "EKCH", "091800Z 0918/1018 16012KT CAVOK");
        var fetcher = new NorthAviMetAdapter(ClientFor(html));

        var (_, taf) = await fetcher.FetchAsync(new[] { "EKCH" });
        
        taf["EKCH"].Should().Be("TAF AMD EKCH 091800Z 0918/1018 16012KT CAVOK");
    }

    [Fact]
    public async Task Parses_TAF_COR_in_body_start()
    {
        // COR ligger i body, ikke i første celle
        var html = Row("TAF", "EKCH", "COR 091802Z 0918/1018 16012KT CAVOK");
        var fetcher = new NorthAviMetAdapter(ClientFor(html));

        var (_, taf) = await fetcher.FetchAsync(new[] { "EKCH" });
        
        taf["EKCH"].Should().Be("TAF COR EKCH 091802Z 0918/1018 16012KT CAVOK");
    }

    [Fact]
    public async Task Fallback_plaintext_with_all_variants()
    {
        var text =
            @"NorthAviMet - 20:00 UTC, August 09, 2025
            
            METAR EKCH 091950Z 17008KT 9999 FEW020 18/15 Q1018=
            SPECI COR EKCH 091955Z 17010KT 9999 FEW020 18/15 Q1018=
            TAF AMD EKCH 091800Z 0918/1018 16012KT CAVOK=
            TAF COR EKCH 091805Z 0918/1018 16012KT CAVOK=";

        var fetcher = new NorthAviMetAdapter(ClientFor(text));

        var (metar, taf) = await fetcher.FetchAsync(new[] { "EKCH" });
        
        metar["EKCH"].Should().Be("METAR COR EKCH 091955Z 17010KT 9999 FEW020 18/15 Q1018");
        taf["EKCH"].Should().Be("TAF COR EKCH 091805Z 0918/1018 16012KT CAVOK");
    }

    [Fact]
    public async Task Handles_multiple_airports()
    {
        var html = Row("METAR", "EKCH", "091920Z 17008KT 9999 FEW020 18/15 Q1018") +
                   Row("TAF", "EKCH", "091714Z 0918/1018 17012KT CAVOK") +
                   Row("METAR", "EKBI", "091920Z 27010KT 9999 SCT040 12/08 Q1020") +
                   Row("TAF AMD", "EKBI", "091800Z 0918/1018 27015KT 9999 BKN030");
        
        var fetcher = new NorthAviMetAdapter(ClientFor(html));

        var (metar, taf) = await fetcher.FetchAsync(new[] { "EKCH", "EKBI" });
        
        metar.Should().HaveCount(2);
        metar["EKCH"].Should().Contain("EKCH");
        metar["EKBI"].Should().Contain("EKBI");
        
        taf.Should().HaveCount(2);
        taf["EKCH"].Should().Be("TAF EKCH 091714Z 0918/1018 17012KT CAVOK");
        taf["EKBI"].Should().Be("TAF AMD EKBI 091800Z 0918/1018 27015KT 9999 BKN030");
    }

    [Fact]
    public async Task Returns_empty_when_no_data()
    {
        var html = "<html><body>No data available</body></html>";
        var fetcher = new NorthAviMetAdapter(ClientFor(html));

        var (metar, taf) = await fetcher.FetchAsync(new[] { "XXXX" });
        
        metar.Should().BeEmpty();
        taf.Should().BeEmpty();
    }
}
