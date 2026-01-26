using FluentAssertions;
using Imagile.Framework.Blazor.ApplicationInsights.Models;
using Imagile.Framework.Blazor.ApplicationInsights.Tests.Mocks;
using Xunit;

namespace Imagile.Framework.Blazor.ApplicationInsights.Tests;

/// <summary>
/// Unit tests for ApplicationInsights class verifying JS interop calls.
/// </summary>
public class ApplicationInsightsTests
{
    [Fact]
    public async Task TrackEvent_CallsCorrectJSMethod()
    {
        // Arrange
        var mockJs = new MockJSRuntime();
        var appInsights = new ApplicationInsights();
        appInsights.InitJSRuntime(mockJs);

        var eventTelemetry = new EventTelemetry
        {
            Name = "TestEvent",
            Properties = new Dictionary<string, object?>
            {
                { "customProperty", "customValue" }
            }
        };

        // Act
        await appInsights.TrackEvent(eventTelemetry);

        // Assert
        mockJs.Invocations.Should().ContainSingle();
        mockJs.Invocations[0].Identifier.Should().Be("appInsights.trackEvent");
        mockJs.Invocations[0].Arguments.Should().HaveCount(1);
    }

    [Fact]
    public async Task TrackPageView_CallsCorrectJSMethod()
    {
        // Arrange
        var mockJs = new MockJSRuntime();
        var appInsights = new ApplicationInsights();
        appInsights.InitJSRuntime(mockJs);

        var pageViewTelemetry = new PageViewTelemetry
        {
            Name = "TestPage",
            Uri = "https://test.local"
        };

        // Act
        await appInsights.TrackPageView(pageViewTelemetry);

        // Assert
        mockJs.Invocations.Should().ContainSingle();
        mockJs.Invocations[0].Identifier.Should().Be("appInsights.trackPageView");
    }

    [Fact]
    public async Task TrackException_CallsCorrectJSMethod()
    {
        // Arrange
        var mockJs = new MockJSRuntime();
        var appInsights = new ApplicationInsights();
        appInsights.InitJSRuntime(mockJs);

        var exceptionTelemetry = new ExceptionTelemetry
        {
            Exception = new Error
            {
                Name = "TestException",
                Message = "Test error message"
            },
            SeverityLevel = SeverityLevel.Error
        };

        // Act
        await appInsights.TrackException(exceptionTelemetry);

        // Assert
        mockJs.Invocations.Should().ContainSingle();
        mockJs.Invocations[0].Identifier.Should().Be("appInsights.trackException");
    }

    [Fact]
    public async Task TrackTrace_CallsCorrectJSMethod()
    {
        // Arrange
        var mockJs = new MockJSRuntime();
        var appInsights = new ApplicationInsights();
        appInsights.InitJSRuntime(mockJs);

        var traceTelemetry = new TraceTelemetry
        {
            Message = "Test trace message",
            SeverityLevel = SeverityLevel.Information
        };

        // Act
        await appInsights.TrackTrace(traceTelemetry);

        // Assert
        mockJs.Invocations.Should().ContainSingle();
        mockJs.Invocations[0].Identifier.Should().Be("appInsights.trackTrace");
    }

    [Fact]
    public async Task AddTelemetryInitializer_CallsBlazorJSMethod()
    {
        // Arrange
        var mockJs = new MockJSRuntime();
        var appInsights = new ApplicationInsights();
        appInsights.InitJSRuntime(mockJs);

        var telemetryItem = new TelemetryItem
        {
            Tags = new Dictionary<string, object?>
            {
                { "ai.cloud.roleInstance", "test-instance" }
            }
        };

        // Act
        await appInsights.AddTelemetryInitializer(telemetryItem);

        // Assert
        mockJs.Invocations.Should().ContainSingle();
        mockJs.Invocations[0].Identifier.Should().Be("blazorApplicationInsights.addTelemetryInitializer");
    }

    [Fact]
    public async Task Flush_CallsCorrectJSMethod()
    {
        // Arrange
        var mockJs = new MockJSRuntime();
        var appInsights = new ApplicationInsights();
        appInsights.InitJSRuntime(mockJs);

        // Act
        await appInsights.Flush();

        // Assert
        mockJs.Invocations.Should().ContainSingle();
        mockJs.Invocations[0].Identifier.Should().Be("appInsights.flush");
    }

    [Fact]
    public async Task TrackMetric_CallsCorrectJSMethod()
    {
        // Arrange
        var mockJs = new MockJSRuntime();
        var appInsights = new ApplicationInsights();
        appInsights.InitJSRuntime(mockJs);

        var metricTelemetry = new MetricTelemetry
        {
            Name = "TestMetric",
            Average = 100.5
        };

        // Act
        await appInsights.TrackMetric(metricTelemetry);

        // Assert
        mockJs.Invocations.Should().ContainSingle();
        mockJs.Invocations[0].Identifier.Should().Be("appInsights.trackMetric");
    }

    [Fact]
    public async Task TrackDependencyData_CallsCorrectJSMethod()
    {
        // Arrange
        var mockJs = new MockJSRuntime();
        var appInsights = new ApplicationInsights();
        appInsights.InitJSRuntime(mockJs);

        var dependencyTelemetry = new DependencyTelemetry
        {
            Id = "test-id",
            Name = "TestDependency",
            Duration = 1000.0 // milliseconds
        };

        // Act
        await appInsights.TrackDependencyData(dependencyTelemetry);

        // Assert
        mockJs.Invocations.Should().ContainSingle();
        mockJs.Invocations[0].Identifier.Should().Be("blazorApplicationInsights.trackDependencyData");
    }
}
