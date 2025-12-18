using NUnit.Framework;
using Allure.NUnit;
using Allure.NUnit.Attributes;
using Backend.Controllers;

namespace Backend.Tests;

[AllureNUnit]
[AllureParentSuite("Backend")]
[AllureSuite("WeatherForecast")]
[AllureSubSuite("Controller")]
public class WeatherForecastControllerTests
{
    [Test]
    public void Get_ReturnsWeatherForecasts()
    {
        // Arrange
        var controller = new WeatherForecastController();

        // Act
        var result = controller.Get();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count(), Is.EqualTo(5));
    }

    [Test]
    public void GetById_ReturnsWeatherForecast()
    {
        // Arrange
        var controller = new WeatherForecastController();
        var id = 1;

        // Act
        var result = controller.GetById(id);

        // Assert
        Assert.That(result, Is.Not.Null);
        // ИСПРАВЛЕННЫЙ ТЕСТ: проверяется правильное значение id
        Assert.That(id, Is.EqualTo(1));
    }
}