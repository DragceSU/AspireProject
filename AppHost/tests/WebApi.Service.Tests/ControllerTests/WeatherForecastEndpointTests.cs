using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;

namespace WebApi.Service.Tests.ControllerTests;

[TestFixture]
public class WeatherForecastEndpointTests
{
    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _factory = new WebApplicationFactory<Program>();
        _client = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    [Test]
    public async Task GetWeatherForecast_ReturnsFiveForecasts()
    {
        var response = await _client.GetAsync("/WeatherForecast");

        response.EnsureSuccessStatusCode();
        var forecasts = await response.Content.ReadFromJsonAsync<List<WeatherForecastResponse>>() ?? [];

        Assert.That(forecasts, Has.Count.EqualTo(5));
        foreach (var forecast in forecasts)
        {
            Assert.That(forecast.Date, Is.Not.EqualTo(default(DateOnly)));
            Assert.That(forecast.TemperatureC, Is.InRange(-60, 60));
            Assert.That(string.IsNullOrWhiteSpace(forecast.Summary), Is.False);
        }
    }

    private sealed record WeatherForecastResponse(DateOnly Date, int TemperatureC, int TemperatureF, string? Summary);
}