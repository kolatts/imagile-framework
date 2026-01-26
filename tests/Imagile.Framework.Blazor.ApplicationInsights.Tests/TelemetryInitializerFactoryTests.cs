using FluentAssertions;
using Imagile.Framework.Blazor.ApplicationInsights.Models;
using Imagile.Framework.Blazor.ApplicationInsights.Telemetry;
using Imagile.Framework.Blazor.ApplicationInsights.Tests.Mocks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Imagile.Framework.Blazor.ApplicationInsights.Tests;

/// <summary>
/// Unit tests for ITelemetryInitializerFactory pattern.
/// </summary>
public class TelemetryInitializerFactoryTests
{
    [Fact]
    public async Task DefaultFactory_ReturnsEmptyTelemetryItem()
    {
        // Arrange
        var factory = new DefaultTelemetryInitializerFactory();

        // Act
        var telemetryItem = await factory.CreateInitializerAsync();

        // Assert
        telemetryItem.Should().NotBeNull();
        telemetryItem.Tags.Should().BeNullOrEmpty();
        telemetryItem.Data.Should().BeNull();
    }

    [Fact]
    public async Task CustomFactory_CanAddTenantId()
    {
        // Arrange
        var factory = new CustomTenantFactory("tenant-123");

        // Act
        var telemetryItem = await factory.CreateInitializerAsync();

        // Assert
        telemetryItem.Should().NotBeNull();
        telemetryItem.Tags.Should().NotBeNull();
        telemetryItem.Tags!.Should().ContainKey("ai.cloud.roleInstance");
        telemetryItem.Tags["ai.cloud.roleInstance"].Should().Be("tenant-123");
    }

    [Fact]
    public async Task CustomFactory_CanAddUserContext()
    {
        // Arrange
        var factory = new CustomUserFactory("user-456");

        // Act
        var telemetryItem = await factory.CreateInitializerAsync();

        // Assert
        telemetryItem.Should().NotBeNull();
        telemetryItem.Tags.Should().NotBeNull();
        telemetryItem.Tags!.Should().ContainKey("ai.user.id");
        telemetryItem.Tags["ai.user.id"].Should().Be("user-456");
    }

    [Fact]
    public void Factory_IsRegisteredInDI_WhenAddBlazorApplicationInsightsCalled()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockJs = new MockJSRuntime();

        services.AddSingleton(mockJs);
        services.AddBlazorApplicationInsights(config =>
        {
            config.ConnectionString = "test-connection-string";
        });

        // Act
        var provider = services.BuildServiceProvider();
        var factory = provider.GetService<ITelemetryInitializerFactory>();

        // Assert
        factory.Should().NotBeNull();
        factory.Should().BeOfType<DefaultTelemetryInitializerFactory>();
    }

    [Fact]
    public void CustomFactory_OverridesDefault_WhenRegisteredFirst()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockJs = new MockJSRuntime();
        var customFactory = new CustomTenantFactory("custom-tenant");

        services.AddSingleton(mockJs);
        services.AddSingleton<ITelemetryInitializerFactory>(customFactory);
        services.AddBlazorApplicationInsights(config =>
        {
            config.ConnectionString = "test-connection-string";
        });

        // Act
        var provider = services.BuildServiceProvider();
        var factory = provider.GetService<ITelemetryInitializerFactory>();

        // Assert
        factory.Should().NotBeNull();
        factory.Should().BeSameAs(customFactory);
    }

    [Fact]
    public async Task CustomFactory_CanCombineMultipleTags()
    {
        // Arrange
        var factory = new CustomCombinedFactory("tenant-789", "user-012");

        // Act
        var telemetryItem = await factory.CreateInitializerAsync();

        // Assert
        telemetryItem.Should().NotBeNull();
        telemetryItem.Tags.Should().NotBeNull();
        telemetryItem.Tags!.Should().ContainKey("ai.cloud.roleInstance");
        telemetryItem.Tags["ai.cloud.roleInstance"].Should().Be("tenant-789");
        telemetryItem.Tags.Should().ContainKey("ai.user.id");
        telemetryItem.Tags["ai.user.id"].Should().Be("user-012");
    }
}

/// <summary>
/// Custom telemetry initializer factory for testing tenant ID injection.
/// </summary>
file class CustomTenantFactory : ITelemetryInitializerFactory
{
    private readonly string _tenantId;

    public CustomTenantFactory(string tenantId)
    {
        _tenantId = tenantId;
    }

    public Task<TelemetryItem> CreateInitializerAsync()
    {
        var telemetryItem = new TelemetryItem
        {
            Tags = new Dictionary<string, object?>
            {
                { "ai.cloud.roleInstance", _tenantId }
            }
        };

        return Task.FromResult(telemetryItem);
    }
}

/// <summary>
/// Custom telemetry initializer factory for testing user context injection.
/// </summary>
file class CustomUserFactory : ITelemetryInitializerFactory
{
    private readonly string _userId;

    public CustomUserFactory(string userId)
    {
        _userId = userId;
    }

    public Task<TelemetryItem> CreateInitializerAsync()
    {
        var telemetryItem = new TelemetryItem
        {
            Tags = new Dictionary<string, object?>
            {
                { "ai.user.id", _userId }
            }
        };

        return Task.FromResult(telemetryItem);
    }
}

/// <summary>
/// Custom telemetry initializer factory for testing combined tags.
/// </summary>
file class CustomCombinedFactory : ITelemetryInitializerFactory
{
    private readonly string _tenantId;
    private readonly string _userId;

    public CustomCombinedFactory(string tenantId, string userId)
    {
        _tenantId = tenantId;
        _userId = userId;
    }

    public Task<TelemetryItem> CreateInitializerAsync()
    {
        var telemetryItem = new TelemetryItem
        {
            Tags = new Dictionary<string, object?>
            {
                { "ai.cloud.roleInstance", _tenantId },
                { "ai.user.id", _userId }
            }
        };

        return Task.FromResult(telemetryItem);
    }
}
