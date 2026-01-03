using Domain.Entities;
using Domain.ValueObjects;
using FluentAssertions;
using Infrastructure.Repositories;
using Xunit;

namespace Tests.Infrastructure;

public class InMemoryAirportRepositoryTests
{
    private readonly InMemoryAirportRepository _sut = new();

    private static Airport CreateAirport(string icao) => new(new AirportInfo
    {
        IcaoId = icao,
        Country = "Denmark"
    });

    [Fact]
    public async Task GetAsync_ReturnsNull_WhenNotFound()
    {
        var result = await _sut.GetAsync("XXXX");

        result.Should().BeNull();
    }

    [Fact]
    public async Task AddAsync_And_GetAsync_Works()
    {
        var airport = CreateAirport("EKCH");

        await _sut.AddAsync(airport);
        var result = await _sut.GetAsync("EKCH");

        result.Should().Be(airport);
    }

    [Fact]
    public async Task GetAsync_IsCaseInsensitive()
    {
        var airport = CreateAirport("EKCH");
        await _sut.AddAsync(airport);

        var result = await _sut.GetAsync("ekch");

        result.Should().Be(airport);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllAirports()
    {
        await _sut.AddAsync(CreateAirport("EKCH"));
        await _sut.AddAsync(CreateAirport("EKBI"));

        var result = await _sut.GetAllAsync();

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllIcaosAsync_ReturnsAllIcaos()
    {
        await _sut.AddAsync(CreateAirport("EKCH"));
        await _sut.AddAsync(CreateAirport("EKBI"));

        var result = await _sut.GetAllIcaosAsync();

        result.Should().Contain(new[] { "EKCH", "EKBI" });
    }

    [Fact]
    public async Task RemoveAsync_RemovesAirport()
    {
        var airport = CreateAirport("EKCH");
        await _sut.AddAsync(airport);

        await _sut.RemoveAsync("EKCH");
        var result = await _sut.GetAsync("EKCH");

        result.Should().BeNull();
    }

    [Fact]
    public async Task RemoveAsync_IsCaseInsensitive()
    {
        await _sut.AddAsync(CreateAirport("EKCH"));

        await _sut.RemoveAsync("ekch");
        var result = await _sut.GetAsync("EKCH");

        result.Should().BeNull();
    }

    [Fact]
    public async Task ExistsAsync_ReturnsTrue_WhenExists()
    {
        await _sut.AddAsync(CreateAirport("EKCH"));

        var result = await _sut.ExistsAsync("EKCH");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_ReturnsFalse_WhenNotExists()
    {
        var result = await _sut.ExistsAsync("XXXX");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsAsync_IsCaseInsensitive()
    {
        await _sut.AddAsync(CreateAirport("EKCH"));

        var result = await _sut.ExistsAsync("ekch");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task AddAsync_DoesNotThrow_OnDuplicate()
    {
        await _sut.AddAsync(CreateAirport("EKCH"));

        var act = async () => await _sut.AddAsync(CreateAirport("EKCH"));

        await act.Should().NotThrowAsync();
    }
}
