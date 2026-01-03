using Domain.Entities;
using Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Tests.Domain;

public class AirportTests
{
    private static AirportInfo CreateTestInfo(string icao = "EKCH") => new()
    {
        IcaoId = icao,
        Country = "Denmark",
        Latitude = 55.6180,
        Longitude = 12.6560,
        Elevation = 17
    };

    private static MetarData CreateMetar(string icao, DateTime reportTime) => new()
    {
        Icao = icao,
        ReportTime = reportTime,
        FetchTime = DateTime.UtcNow,
        RawMetar = $"METAR {icao} 031050Z 27008KT 9999 FEW040 08/02 Q1024",
        Type = MetarType.Metar
    };

    private static TafData CreateTaf(string icao, DateTime reportTime, TafType type = TafType.Taf) => new()
    {
        Icao = icao,
        ReportTime = reportTime,
        FetchTime = DateTime.UtcNow,
        RawTaf = $"TAF {icao} 031100Z 0312/0412 27010KT 9999 FEW040",
        Type = type
    };

    [Fact]
    public void Constructor_SetsInfoAndIcao()
    {
        var info = CreateTestInfo("EKCH");
        
        var airport = new Airport(info);
        
        airport.Info.Should().Be(info);
        airport.Icao.Should().Be("EKCH");
    }

    [Fact]
    public void Constructor_ThrowsOnNullInfo()
    {
        Action act = () => new Airport(null!);
        
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddMetar_ReturnsTrue_WhenNew()
    {
        var airport = new Airport(CreateTestInfo("EKCH"));
        var metar = CreateMetar("EKCH", DateTime.UtcNow);
        
        var result = airport.AddMetar(metar);
        
        result.Should().BeTrue();
    }

    [Fact]
    public void AddMetar_ReturnsFalse_WhenDuplicate()
    {
        var airport = new Airport(CreateTestInfo("EKCH"));
        var reportTime = DateTime.UtcNow;
        var metar1 = CreateMetar("EKCH", reportTime);
        var metar2 = CreateMetar("EKCH", reportTime);
        
        airport.AddMetar(metar1);
        var result = airport.AddMetar(metar2);
        
        result.Should().BeFalse();
    }

    [Fact]
    public void AddMetar_ThrowsOnWrongIcao()
    {
        var airport = new Airport(CreateTestInfo("EKCH"));
        var metar = CreateMetar("EKBI", DateTime.UtcNow);
        
        Action act = () => airport.AddMetar(metar);
        
        act.Should().Throw<ArgumentException>().WithMessage("*EKBI*EKCH*");
    }

    [Fact]
    public void AddTaf_ReturnsTrue_WhenNew()
    {
        var airport = new Airport(CreateTestInfo("EKCH"));
        var taf = CreateTaf("EKCH", DateTime.UtcNow);
        
        var result = airport.AddTaf(taf);
        
        result.Should().BeTrue();
    }

    [Fact]
    public void AddTaf_ReturnsFalse_WhenDuplicate()
    {
        var airport = new Airport(CreateTestInfo("EKCH"));
        var reportTime = DateTime.UtcNow;
        
        airport.AddTaf(CreateTaf("EKCH", reportTime));
        var result = airport.AddTaf(CreateTaf("EKCH", reportTime));
        
        result.Should().BeFalse();
    }

    [Fact]
    public void GetLatestMetar_ReturnsNull_WhenEmpty()
    {
        var airport = new Airport(CreateTestInfo());
        
        airport.GetLatestMetar().Should().BeNull();
    }

    [Fact]
    public void GetLatestMetar_ReturnsMostRecent()
    {
        var airport = new Airport(CreateTestInfo("EKCH"));
        var older = CreateMetar("EKCH", DateTime.UtcNow.AddMinutes(-30));
        var newer = CreateMetar("EKCH", DateTime.UtcNow);
        
        airport.AddMetar(older);
        airport.AddMetar(newer);
        
        airport.GetLatestMetar().Should().Be(newer);
    }

    [Fact]
    public void GetLatestTaf_ReturnsNull_WhenEmpty()
    {
        var airport = new Airport(CreateTestInfo());
        
        airport.GetLatestTaf().Should().BeNull();
    }

    [Fact]
    public void GetLatestTaf_ReturnsMostRecent()
    {
        var airport = new Airport(CreateTestInfo("EKCH"));
        var older = CreateTaf("EKCH", DateTime.UtcNow.AddHours(-6));
        var newer = CreateTaf("EKCH", DateTime.UtcNow);
        
        airport.AddTaf(older);
        airport.AddTaf(newer);
        
        airport.GetLatestTaf().Should().Be(newer);
    }

    [Fact]
    public void GetAllMetars_ReturnsOrderedByReportTimeDescending()
    {
        var airport = new Airport(CreateTestInfo("EKCH"));
        var t1 = DateTime.UtcNow.AddMinutes(-60);
        var t2 = DateTime.UtcNow.AddMinutes(-30);
        var t3 = DateTime.UtcNow;
        
        airport.AddMetar(CreateMetar("EKCH", t2));
        airport.AddMetar(CreateMetar("EKCH", t1));
        airport.AddMetar(CreateMetar("EKCH", t3));
        
        var all = airport.GetAllMetars();
        
        all.Should().HaveCount(3);
        all[0].ReportTime.Should().Be(t3);
        all[1].ReportTime.Should().Be(t2);
        all[2].ReportTime.Should().Be(t1);
    }

    [Fact]
    public void GetAllTafs_ReturnsOrderedByReportTimeDescending()
    {
        var airport = new Airport(CreateTestInfo("EKCH"));
        var t1 = DateTime.UtcNow.AddHours(-12);
        var t2 = DateTime.UtcNow.AddHours(-6);
        var t3 = DateTime.UtcNow;
        
        airport.AddTaf(CreateTaf("EKCH", t2));
        airport.AddTaf(CreateTaf("EKCH", t1));
        airport.AddTaf(CreateTaf("EKCH", t3));
        
        var all = airport.GetAllTafs();
        
        all.Should().HaveCount(3);
        all[0].ReportTime.Should().Be(t3);
        all[1].ReportTime.Should().Be(t2);
        all[2].ReportTime.Should().Be(t1);
    }
}
