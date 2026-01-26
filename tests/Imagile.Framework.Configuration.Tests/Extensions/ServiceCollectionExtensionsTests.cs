using Azure.Core;
using Azure.Security.KeyVault.Secrets;
using FluentAssertions;
using Imagile.Framework.Configuration.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Imagile.Framework.Configuration.Tests.Extensions;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddFrameworkConfiguration_WithKeyVault_RegistersTokenCredential()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        // Act
        services.AddFrameworkConfiguration(configuration, builder => builder
            .WithKeyVault(new Uri("https://test.vault.azure.net/")));

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var tokenCredential = serviceProvider.GetService<TokenCredential>();
        tokenCredential.Should().NotBeNull();
    }

    [Fact]
    public void AddFrameworkConfiguration_WithKeyVault_RegistersSecretClient()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        // Act
        services.AddFrameworkConfiguration(configuration, builder => builder
            .WithKeyVault(new Uri("https://test.vault.azure.net/")));

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var secretClient = serviceProvider.GetService<SecretClient>();
        secretClient.Should().NotBeNull();
    }

    [Fact]
    public void AddFrameworkConfiguration_WithKeyVaultString_RegistersServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        // Act
        services.AddFrameworkConfiguration(configuration, builder => builder
            .WithKeyVault("https://test.vault.azure.net/"));

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        serviceProvider.GetService<TokenCredential>().Should().NotBeNull();
        serviceProvider.GetService<SecretClient>().Should().NotBeNull();
    }

    [Fact]
    public void AddFrameworkConfiguration_WithKeyVaultAndManagedIdentity_RegistersServicesWithClientId()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();
        var managedIdentityClientId = Guid.NewGuid().ToString();

        // Act
        services.AddFrameworkConfiguration(configuration, builder => builder
            .WithKeyVault(new Uri("https://test.vault.azure.net/"), managedIdentityClientId));

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var tokenCredential = serviceProvider.GetService<TokenCredential>();
        tokenCredential.Should().NotBeNull();
    }

    [Fact]
    public void AddFrameworkConfiguration_WithValidation_EnablesOptionsServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        // Act
        services.AddFrameworkConfiguration(configuration, builder => builder
            .WithValidation());

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        // Verify that options services were added
        serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<object>>().Should().NotBeNull();
    }

    [Fact]
    public void AddFrameworkConfiguration_Chainable_ReturnsServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        // Act
        var result = services.AddFrameworkConfiguration(configuration, builder => builder
            .WithKeyVault(new Uri("https://test.vault.azure.net/"))
            .WithValidation());

        // Assert
        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AddFrameworkConfiguration_WithNullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;
        var configuration = new ConfigurationBuilder().Build();

        // Act
        var act = () => services!.AddFrameworkConfiguration(configuration, builder => { });

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("services");
    }

    [Fact]
    public void AddFrameworkConfiguration_WithNullConfiguration_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        IConfiguration? configuration = null;

        // Act
        var act = () => services.AddFrameworkConfiguration(configuration!, builder => { });

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("configuration");
    }

    [Fact]
    public void AddFrameworkConfiguration_WithNullConfigure_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();
        Action<FrameworkConfigurationBuilder>? configure = null;

        // Act
        var act = () => services.AddFrameworkConfiguration(configuration, configure!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("configure");
    }

    [Fact]
    public void WithKeyVault_WithNullUri_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        // Act
        var act = () => services.AddFrameworkConfiguration(configuration, builder => builder
            .WithKeyVault((Uri)null!));

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void WithKeyVault_WithNullUrl_ThrowsArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        // Act
        var act = () => services.AddFrameworkConfiguration(configuration, builder => builder
            .WithKeyVault((string)null!));

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void WithKeyVault_WithEmptyUrl_ThrowsArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        // Act
        var act = () => services.AddFrameworkConfiguration(configuration, builder => builder
            .WithKeyVault(string.Empty));

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void WithKeyVault_WithWhitespaceUrl_ThrowsArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        // Act
        var act = () => services.AddFrameworkConfiguration(configuration, builder => builder
            .WithKeyVault("   "));

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddFrameworkConfiguration_WithMultipleFeatures_ConfiguresAllFeatures()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        // Act
        services.AddFrameworkConfiguration(configuration, builder => builder
            .WithKeyVault(new Uri("https://test.vault.azure.net/"))
            .WithValidation());

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        serviceProvider.GetService<TokenCredential>().Should().NotBeNull();
        serviceProvider.GetService<SecretClient>().Should().NotBeNull();
        serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<object>>().Should().NotBeNull();
    }
}
