using FluentAssertions;
using Imagile.Framework.Blazor.ApplicationInsights.Components;
using Imagile.Framework.Blazor.ApplicationInsights.Interfaces;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Imagile.Framework.Blazor.ApplicationInsights.Tests;

/// <summary>
/// Unit tests for ApplicationInsightsErrorBoundary structure and dependencies.
/// Note: Full end-to-end testing requires Blazor rendering infrastructure (integration tests).
/// These tests verify the component structure and ensure it can be instantiated.
/// </summary>
public class ErrorBoundaryTests
{
    [Fact]
    public void ErrorBoundary_CanBeInstantiated()
    {
        // Arrange & Act
        var errorBoundary = new ApplicationInsightsErrorBoundary();

        // Assert
        errorBoundary.Should().NotBeNull();
        errorBoundary.Should().BeAssignableTo<Microsoft.AspNetCore.Components.Web.ErrorBoundary>();
    }

    [Fact]
    public void ErrorBoundary_HasApplicationInsightsProperty()
    {
        // Arrange
        var errorBoundary = new ApplicationInsightsErrorBoundary();

        // Act
        var property = typeof(ApplicationInsightsErrorBoundary)
            .GetProperty("ApplicationInsights", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        // Assert
        property.Should().NotBeNull();
        property!.PropertyType.Should().Be(typeof(IApplicationInsights));
    }

    [Fact]
    public void ErrorBoundary_HasLoggerProperty()
    {
        // Arrange
        var errorBoundary = new ApplicationInsightsErrorBoundary();

        // Act
        var property = typeof(ApplicationInsightsErrorBoundary)
            .GetProperty("Logger", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        // Assert
        property.Should().NotBeNull();
        property!.PropertyType.Should().Be(typeof(ILogger<ApplicationInsightsErrorBoundary>));
    }

    [Fact]
    public void ErrorBoundary_OverridesOnErrorAsync()
    {
        // Arrange & Act
        var method = typeof(ApplicationInsightsErrorBoundary)
            .GetMethod("OnErrorAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task));
        var parameters = method.GetParameters();
        parameters.Should().ContainSingle();
        parameters[0].ParameterType.Should().Be(typeof(Exception));
    }
}
