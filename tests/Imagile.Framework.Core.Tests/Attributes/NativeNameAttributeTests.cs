using FluentAssertions;
using Imagile.Framework.Core.Attributes;
using Imagile.Framework.Core.Extensions;
using Xunit;

namespace Imagile.Framework.Core.Tests.Attributes;

/// <summary>
/// Tests for NativeNameAttribute construction and retrieval.
/// </summary>
public class NativeNameAttributeTests
{
    private enum Language
    {
        [NativeName("English")]
        English,

        [NativeName("Español")]
        Spanish,

        [NativeName("Français")]
        French,

        NoNativeName
    }

    [Fact]
    public void Constructor_StoresNativeName()
    {
        var attribute = new NativeNameAttribute("日本語");

        attribute.Name.Should().Be("日本語");
    }

    [Fact]
    public void GetNativeName_ReturnsCorrectValue()
    {
        var nativeName = Language.Spanish.GetNativeName();

        nativeName.Should().Be("Español");
    }

    [Fact]
    public void GetNativeName_SupportsUnicode()
    {
        var nativeName = Language.French.GetNativeName();

        nativeName.Should().Be("Français");
    }

    [Fact]
    public void GetNativeName_ReturnsFallback_WhenNoAttribute()
    {
        var nativeName = Language.NoNativeName.GetNativeName();

        nativeName.Should().Be("NoNativeName");
    }
}
