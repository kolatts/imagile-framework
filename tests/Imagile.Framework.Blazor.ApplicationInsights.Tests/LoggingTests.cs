using FluentAssertions;
using Imagile.Framework.Blazor.ApplicationInsights.Logging;
using Imagile.Framework.Blazor.ApplicationInsights.Models;
using Imagile.Framework.Blazor.ApplicationInsights.Tests.Mocks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace Imagile.Framework.Blazor.ApplicationInsights.Tests;

/// <summary>
/// Unit tests for ILoggerProvider integration with Application Insights.
/// </summary>
public class LoggingTests
{
    [Fact]
    public void LoggerProvider_CreatesLogger()
    {
        // Arrange
        var mockAppInsights = new MockApplicationInsights();
        var options = new ApplicationInsightsLoggerOptions
        {
            MinLogLevel = LogLevel.Trace
        };
        var optionsMonitor = new DummyOptionsMonitor<ApplicationInsightsLoggerOptions>(options);
        var provider = new ApplicationInsightsLoggerProvider(mockAppInsights, optionsMonitor);

        // Act
        var logger = provider.CreateLogger("TestCategory");

        // Assert
        logger.Should().NotBeNull();
        logger.Should().BeOfType<ApplicationInsightsLogger>();
    }

    [Fact]
    public void Logger_LogInformation_CallsTrackTrace()
    {
        // Arrange
        var mockAppInsights = new MockApplicationInsights();
        var options = new ApplicationInsightsLoggerOptions
        {
            MinLogLevel = LogLevel.Trace,
            IncludeCategoryName = false
        };
        var optionsMonitor = new DummyOptionsMonitor<ApplicationInsightsLoggerOptions>(options);
        var provider = new ApplicationInsightsLoggerProvider(mockAppInsights, optionsMonitor);
        var logger = provider.CreateLogger("TestCategory");

        // Act
        logger.LogInformation("Test message");

        // Assert
        mockAppInsights.Calls.Should().ContainSingle();
        mockAppInsights.Calls[0].MethodName.Should().Be(nameof(mockAppInsights.TrackTrace));

        var traceTelemetry = mockAppInsights.Calls[0].Argument as TraceTelemetry;
        traceTelemetry.Should().NotBeNull();
        traceTelemetry!.Message.Should().Be("Test message");
        traceTelemetry.SeverityLevel.Should().Be(SeverityLevel.Information);
    }

    [Fact]
    public void Logger_BelowMinLevel_DoesNotTrack()
    {
        // Arrange
        var mockAppInsights = new MockApplicationInsights();
        var options = new ApplicationInsightsLoggerOptions
        {
            MinLogLevel = LogLevel.Warning
        };
        var optionsMonitor = new DummyOptionsMonitor<ApplicationInsightsLoggerOptions>(options);
        var provider = new ApplicationInsightsLoggerProvider(mockAppInsights, optionsMonitor);
        var logger = provider.CreateLogger("TestCategory");

        // Act
        logger.LogInformation("This should not be tracked");

        // Assert
        mockAppInsights.Calls.Should().BeEmpty();
    }

    [Fact]
    public void Logger_AtMinLevel_TracksMessage()
    {
        // Arrange
        var mockAppInsights = new MockApplicationInsights();
        var options = new ApplicationInsightsLoggerOptions
        {
            MinLogLevel = LogLevel.Warning
        };
        var optionsMonitor = new DummyOptionsMonitor<ApplicationInsightsLoggerOptions>(options);
        var provider = new ApplicationInsightsLoggerProvider(mockAppInsights, optionsMonitor);
        var logger = provider.CreateLogger("TestCategory");

        // Act
        logger.LogWarning("This should be tracked");

        // Assert
        mockAppInsights.Calls.Should().ContainSingle();
        var traceTelemetry = mockAppInsights.Calls[0].Argument as TraceTelemetry;
        traceTelemetry.Should().NotBeNull();
        traceTelemetry!.SeverityLevel.Should().Be(SeverityLevel.Warning);
    }

    [Fact]
    public void Logger_IncludesCategory_InProperties()
    {
        // Arrange
        var mockAppInsights = new MockApplicationInsights();
        var options = new ApplicationInsightsLoggerOptions
        {
            MinLogLevel = LogLevel.Trace,
            IncludeCategoryName = true
        };
        var optionsMonitor = new DummyOptionsMonitor<ApplicationInsightsLoggerOptions>(options);
        var provider = new ApplicationInsightsLoggerProvider(mockAppInsights, optionsMonitor);
        var logger = provider.CreateLogger("MyTestCategory");

        // Act
        logger.LogInformation("Test message");

        // Assert
        mockAppInsights.Calls.Should().ContainSingle();
        var traceTelemetry = mockAppInsights.Calls[0].Argument as TraceTelemetry;
        traceTelemetry.Should().NotBeNull();
        traceTelemetry!.Properties.Should().ContainKey("CategoryName");
        traceTelemetry.Properties["CategoryName"].Should().Be("MyTestCategory");
    }

    [Fact]
    public void Logger_LogError_CallsTrackException()
    {
        // Arrange
        var mockAppInsights = new MockApplicationInsights();
        var options = new ApplicationInsightsLoggerOptions
        {
            MinLogLevel = LogLevel.Trace
        };
        var optionsMonitor = new DummyOptionsMonitor<ApplicationInsightsLoggerOptions>(options);
        var provider = new ApplicationInsightsLoggerProvider(mockAppInsights, optionsMonitor);
        var logger = provider.CreateLogger("TestCategory");

        var exception = new InvalidOperationException("Test exception");

        // Act
        logger.LogError(exception, "Error occurred");

        // Assert
        mockAppInsights.Calls.Should().ContainSingle();
        mockAppInsights.Calls[0].MethodName.Should().Be(nameof(mockAppInsights.TrackException));

        var exceptionTelemetry = mockAppInsights.Calls[0].Argument as ExceptionTelemetry;
        exceptionTelemetry.Should().NotBeNull();
        exceptionTelemetry!.Exception.Should().NotBeNull();
        exceptionTelemetry.Exception.Name.Should().Be("InvalidOperationException");
        exceptionTelemetry.SeverityLevel.Should().Be(SeverityLevel.Error);
    }

    [Fact]
    public void Logger_IncludesScopes_WhenEnabled()
    {
        // Arrange
        var mockAppInsights = new MockApplicationInsights();
        var options = new ApplicationInsightsLoggerOptions
        {
            MinLogLevel = LogLevel.Trace,
            IncludeScopes = true,
            IncludeCategoryName = false
        };
        var optionsMonitor = new DummyOptionsMonitor<ApplicationInsightsLoggerOptions>(options);
        var provider = new ApplicationInsightsLoggerProvider(mockAppInsights, optionsMonitor);
        var logger = provider.CreateLogger("TestCategory");

        // Act
        using (logger.BeginScope("OuterScope"))
        {
            using (logger.BeginScope("InnerScope"))
            {
                logger.LogInformation("Test message");
            }
        }

        // Assert
        mockAppInsights.Calls.Should().ContainSingle();
        var traceTelemetry = mockAppInsights.Calls[0].Argument as TraceTelemetry;
        traceTelemetry.Should().NotBeNull();
        traceTelemetry!.Properties.Should().ContainKey("Scope");
        traceTelemetry.Properties["Scope"].Should().NotBeNull();
        traceTelemetry.Properties["Scope"]!.ToString().Should().Contain("OuterScope");
        traceTelemetry.Properties["Scope"]!.ToString().Should().Contain("InnerScope");
    }

    [Theory]
    [InlineData(LogLevel.Trace, SeverityLevel.Verbose)]
    [InlineData(LogLevel.Debug, SeverityLevel.Verbose)]
    [InlineData(LogLevel.Information, SeverityLevel.Information)]
    [InlineData(LogLevel.Warning, SeverityLevel.Warning)]
    [InlineData(LogLevel.Error, SeverityLevel.Error)]
    [InlineData(LogLevel.Critical, SeverityLevel.Critical)]
    public void Logger_MapsLogLevelToSeverityLevel(LogLevel logLevel, SeverityLevel expectedSeverity)
    {
        // Arrange
        var mockAppInsights = new MockApplicationInsights();
        var options = new ApplicationInsightsLoggerOptions
        {
            MinLogLevel = LogLevel.Trace
        };
        var optionsMonitor = new DummyOptionsMonitor<ApplicationInsightsLoggerOptions>(options);
        var provider = new ApplicationInsightsLoggerProvider(mockAppInsights, optionsMonitor);
        var logger = provider.CreateLogger("TestCategory");

        // Act
        logger.Log(logLevel, "Test message");

        // Assert
        mockAppInsights.Calls.Should().ContainSingle();
        var traceTelemetry = mockAppInsights.Calls[0].Argument as TraceTelemetry;
        traceTelemetry.Should().NotBeNull();
        traceTelemetry!.SeverityLevel.Should().Be(expectedSeverity);
    }
}
