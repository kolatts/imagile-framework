using FluentAssertions;
using Imagile.Framework.Core.Attributes;
using Imagile.Framework.Core.Extensions;
using Xunit;

namespace Imagile.Framework.Core.Tests.Attributes;

/// <summary>
/// Tests for HostedAttribute construction and retrieval.
/// </summary>
public class HostedAttributeTests
{
    private enum Environment
    {
        [Hosted(ApiUrl = "https://api.dev.example.com", WebUrl = "https://dev.example.com")]
        Development,

        [Hosted(ApiUrl = "https://api.example.com", WebUrl = "https://example.com")]
        Production,

        [Hosted(ApiUrl = "https://api.qa.example.com")]
        QaApiOnly,

        NoHosted
    }

    [Fact]
    public void Initializer_StoresApiUrl()
    {
        var attribute = new HostedAttribute { ApiUrl = "https://api.test.com", WebUrl = "https://test.com" };

        attribute.ApiUrl.Should().Be("https://api.test.com");
    }

    [Fact]
    public void Initializer_StoresWebUrl()
    {
        var attribute = new HostedAttribute { ApiUrl = "https://api.test.com", WebUrl = "https://test.com" };

        attribute.WebUrl.Should().Be("https://test.com");
    }

    [Fact]
    public void GetHostedUrls_ReturnsBothUrls()
    {
        var (apiUrl, webUrl) = Environment.Development.GetHostedUrls();

        apiUrl.Should().Be("https://api.dev.example.com");
        webUrl.Should().Be("https://dev.example.com");
    }

    [Fact]
    public void GetHostedUrls_HandlesNullWebUrl()
    {
        var (apiUrl, webUrl) = Environment.QaApiOnly.GetHostedUrls();

        apiUrl.Should().Be("https://api.qa.example.com");
        webUrl.Should().BeNull();
    }

    [Fact]
    public void GetHostedUrls_ReturnsNulls_WhenNoAttribute()
    {
        var (apiUrl, webUrl) = Environment.NoHosted.GetHostedUrls();

        apiUrl.Should().BeNull();
        webUrl.Should().BeNull();
    }
}
