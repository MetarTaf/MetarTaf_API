using Application.DTOs;
using Application.Interfaces;
using Application.UseCases;
using Domain.Entities;
using Domain.Ports;
using Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Tests.Application;

public class WeatherServiceTests
{
    private readonly IAirportRepository _airportRepository;
    private readonly IAirportInfoProvider _airportInfoProvider;
    private readonly IOpmetFetcher _opmetFetcher;
    private readonly IWeatherNotifier _notifier;
    private readonly IMetarParser _metarParser;
    private readonly ITafParser _tafParser;
    private readonly WeatherService _sut;

    public WeatherServiceTests()
    {
        _airportRepository = Substitute.For<IAirportRepository>();
        _airportInfoProvider = Substitute.For<IAirportInfoProvider>();
        _opmetFetcher = Substitute.For<IOpmetFetcher>();
        _notifier = Substitute.For<IWeatherNotifier>();
        _metarParser = Substitute.For<IMetarParser>();
        _tafParser = Substitute.For<ITafParser>();

        _sut = new WeatherService(
            _airportRepository,
            _airportInfoProvider,
            _opmetFetcher,
            _notifier,
            _metarParser,
            _tafParser);
    }

    private static AirportInfo CreateInfo(string icao) => new()
    {
        IcaoId = icao,
        Country = "Denmark"
    };

    private static MetarData CreateMetarData(string icao) => new()
    {
        Icao = icao,
        ReportTime = DateTime.UtcNow,
        FetchTime = DateTime.UtcNow,
        RawMetar = $"METAR {icao} 031050Z 27008KT 9999 FEW040 08/02 Q1024",
        Type = MetarType.Metar
    };

    private static TafData CreateTafData(string icao) => new()
    {
        Icao = icao,
        ReportTime = DateTime.UtcNow,
        FetchTime = DateTime.UtcNow,
        RawTaf = $"TAF {icao} 031100Z 0312/0412 27010KT 9999 FEW040",
        Type = TafType.Taf
    };

    #region SubscribeAsync

    [Fact]
    public async Task SubscribeAsync_ReturnsNull_WhenAirportNotFound()
    {
        _airportRepository.ExistsAsync("XXXX").Returns(false);
        _airportInfoProvider.GetByIcaoAsync("XXXX").Returns((AirportInfo?)null);

        var result = await _sut.SubscribeAsync("XXXX");

        result.Should().BeNull();
    }

    [Fact]
    public async Task SubscribeAsync_ReturnsExisting_WhenAlreadyTracked()
    {
        var existing = new Airport(CreateInfo("EKCH"));
        _airportRepository.ExistsAsync("EKCH").Returns(true);
        _airportRepository.GetAsync("EKCH").Returns(existing);

        var result = await _sut.SubscribeAsync("EKCH");

        result.Should().NotBeNull();
        result!.Icao.Should().Be("EKCH");
        await _airportRepository.DidNotReceive().AddAsync(Arg.Any<Airport>());
    }

    [Fact]
    public async Task SubscribeAsync_CreatesNewAirport_WhenNotTracked()
    {
        _airportRepository.ExistsAsync("EKCH").Returns(false);
        _airportInfoProvider.GetByIcaoAsync("EKCH").Returns(CreateInfo("EKCH"));
        _opmetFetcher.FetchAsync(Arg.Any<IEnumerable<string>>())
            .Returns((new Dictionary<string, string>(), new Dictionary<string, string>()));

        var result = await _sut.SubscribeAsync("EKCH");

        result.Should().NotBeNull();
        result!.Icao.Should().Be("EKCH");
        await _airportRepository.Received(1).AddAsync(Arg.Is<Airport>(a => a.Icao == "EKCH"));
    }

    [Fact]
    public async Task SubscribeAsync_NormalizesIcaoToUppercase()
    {
        _airportRepository.ExistsAsync("EKCH").Returns(false);
        _airportInfoProvider.GetByIcaoAsync("EKCH").Returns(CreateInfo("EKCH"));
        _opmetFetcher.FetchAsync(Arg.Any<IEnumerable<string>>())
            .Returns((new Dictionary<string, string>(), new Dictionary<string, string>()));

        var result = await _sut.SubscribeAsync("  ekch  ");

        result.Should().NotBeNull();
        result!.Icao.Should().Be("EKCH");
    }

    [Fact]
    public async Task SubscribeAsync_FetchesDataImmediately()
    {
        _airportRepository.ExistsAsync("EKCH").Returns(false);
        _airportInfoProvider.GetByIcaoAsync("EKCH").Returns(CreateInfo("EKCH"));
        _opmetFetcher.FetchAsync(Arg.Any<IEnumerable<string>>())
            .Returns((new Dictionary<string, string>(), new Dictionary<string, string>()));

        await _sut.SubscribeAsync("EKCH");

        await _opmetFetcher.Received(1).FetchAsync(Arg.Is<IEnumerable<string>>(x => x.Contains("EKCH")));
    }

    #endregion

    #region UnsubscribeAsync

    [Fact]
    public async Task UnsubscribeAsync_ReturnsTrue_WhenExists()
    {
        _airportRepository.ExistsAsync("EKCH").Returns(true);

        var result = await _sut.UnsubscribeAsync("EKCH");

        result.Should().BeTrue();
        await _airportRepository.Received(1).RemoveAsync("EKCH");
    }

    [Fact]
    public async Task UnsubscribeAsync_ReturnsFalse_WhenNotExists()
    {
        _airportRepository.ExistsAsync("EKCH").Returns(false);

        var result = await _sut.UnsubscribeAsync("EKCH");

        result.Should().BeFalse();
        await _airportRepository.DidNotReceive().RemoveAsync(Arg.Any<string>());
    }

    #endregion

    #region GetAirportAsync

    [Fact]
    public async Task GetAirportAsync_ReturnsNull_WhenNotFound()
    {
        _airportRepository.GetAsync("XXXX").Returns((Airport?)null);

        var result = await _sut.GetAirportAsync("XXXX");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAirportAsync_ReturnsDto_WhenFound()
    {
        var airport = new Airport(CreateInfo("EKCH"));
        _airportRepository.GetAsync("EKCH").Returns(airport);

        var result = await _sut.GetAirportAsync("EKCH");

        result.Should().NotBeNull();
        result!.Icao.Should().Be("EKCH");
        result.Country.Should().Be("Denmark");
    }

    #endregion

    #region GetAllAirportsAsync

    [Fact]
    public async Task GetAllAirportsAsync_ReturnsEmptyList_WhenNoAirports()
    {
        _airportRepository.GetAllAsync().Returns(new List<Airport>());

        var result = await _sut.GetAllAirportsAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAirportsAsync_ReturnsDtos()
    {
        var airports = new List<Airport>
        {
            new(CreateInfo("EKCH")),
            new(CreateInfo("EKBI"))
        };
        _airportRepository.GetAllAsync().Returns(airports);

        var result = await _sut.GetAllAirportsAsync();

        result.Should().HaveCount(2);
        result.Select(a => a.Icao).Should().Contain(new[] { "EKCH", "EKBI" });
    }

    #endregion

    #region FetchAndNotifyAsync

    [Fact]
    public async Task FetchAndNotifyAsync_DoesNothing_WhenNoAirports()
    {
        _airportRepository.GetAllIcaosAsync().Returns(new List<string>());

        await _sut.FetchAndNotifyAsync();

        await _opmetFetcher.DidNotReceive().FetchAsync(Arg.Any<IEnumerable<string>>());
    }

    [Fact]
    public async Task FetchAndNotifyAsync_FetchesForAllTrackedAirports()
    {
        var icaos = new List<string> { "EKCH", "EKBI" };
        _airportRepository.GetAllIcaosAsync().Returns(icaos);
        _airportRepository.GetAsync("EKCH").Returns(new Airport(CreateInfo("EKCH")));
        _airportRepository.GetAsync("EKBI").Returns(new Airport(CreateInfo("EKBI")));
        _opmetFetcher.FetchAsync(Arg.Any<IEnumerable<string>>())
            .Returns((new Dictionary<string, string>(), new Dictionary<string, string>()));

        await _sut.FetchAndNotifyAsync();

        await _opmetFetcher.Received(1).FetchAsync(Arg.Is<IEnumerable<string>>(x => 
            x.Contains("EKCH") && x.Contains("EKBI")));
    }

    [Fact]
    public async Task FetchAndNotifyAsync_NotifiesOnNewMetar()
    {
        var airport = new Airport(CreateInfo("EKCH"));
        var icaos = new List<string> { "EKCH" };
        var rawMetar = "METAR EKCH 031050Z 27008KT 9999 FEW040 08/02 Q1024";
        var metarData = CreateMetarData("EKCH");

        _airportRepository.GetAllIcaosAsync().Returns(icaos);
        _airportRepository.GetAsync("EKCH").Returns(airport);
        _opmetFetcher.FetchAsync(Arg.Any<IEnumerable<string>>())
            .Returns((
                new Dictionary<string, string> { ["EKCH"] = rawMetar },
                new Dictionary<string, string>()
            ));
        _metarParser.Parse(rawMetar).Returns(metarData);

        await _sut.FetchAndNotifyAsync();

        await _notifier.Received(1).NotifyManyAsync(
            Arg.Is<IEnumerable<WeatherUpdateDto>>(updates => 
                updates.Any(u => u.Icao == "EKCH" && u.NewMetar != null)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FetchAndNotifyAsync_NotifiesOnNewTaf()
    {
        var airport = new Airport(CreateInfo("EKCH"));
        var icaos = new List<string> { "EKCH" };
        var rawTaf = "TAF EKCH 031100Z 0312/0412 27010KT 9999 FEW040";
        var tafData = CreateTafData("EKCH");

        _airportRepository.GetAllIcaosAsync().Returns(icaos);
        _airportRepository.GetAsync("EKCH").Returns(airport);
        _opmetFetcher.FetchAsync(Arg.Any<IEnumerable<string>>())
            .Returns((
                new Dictionary<string, string>(),
                new Dictionary<string, string> { ["EKCH"] = rawTaf }
            ));
        _tafParser.Parse(rawTaf).Returns(tafData);

        await _sut.FetchAndNotifyAsync();

        await _notifier.Received(1).NotifyManyAsync(
            Arg.Is<IEnumerable<WeatherUpdateDto>>(updates => 
                updates.Any(u => u.Icao == "EKCH" && u.NewTaf != null)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FetchAndNotifyAsync_DoesNotNotify_WhenNoNewData()
    {
        var airport = new Airport(CreateInfo("EKCH"));
        var metarData = CreateMetarData("EKCH");
        airport.AddMetar(metarData); // Allerede tilføjet

        var icaos = new List<string> { "EKCH" };
        var rawMetar = "METAR EKCH 031050Z 27008KT 9999 FEW040 08/02 Q1024";

        _airportRepository.GetAllIcaosAsync().Returns(icaos);
        _airportRepository.GetAsync("EKCH").Returns(airport);
        _opmetFetcher.FetchAsync(Arg.Any<IEnumerable<string>>())
            .Returns((
                new Dictionary<string, string> { ["EKCH"] = rawMetar },
                new Dictionary<string, string>()
            ));
        _metarParser.Parse(rawMetar).Returns(metarData); // Samme data = AddMetar returnerer false

        await _sut.FetchAndNotifyAsync();

        await _notifier.DidNotReceive().NotifyManyAsync(
            Arg.Any<IEnumerable<WeatherUpdateDto>>(),
            Arg.Any<CancellationToken>());
    }

    #endregion
}
