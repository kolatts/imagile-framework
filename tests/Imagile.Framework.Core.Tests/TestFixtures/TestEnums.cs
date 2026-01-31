using Imagile.Framework.Core.Attributes;

namespace Imagile.Framework.Core.Tests.TestFixtures;

/// <summary>
/// Test enum demonstrating various attribute combinations for testing.
/// </summary>
public enum TestEnumTypes
{
    [Requires<TestEnumAlternativeTypes>(TestEnumAlternativeTypes.Z, TestEnumAlternativeTypes.Y)]
    A,

    [Includes<TestEnumTypes>(A)]
    B,

    [Includes<TestEnumTypes>(B)]
    C,

    [Associated<TestEnumAlternativeTypes>(TestEnumAlternativeTypes.X)]
    D,

    [DerivedIncludes(D)]
    E,

    [Requires<TestEnumTypes>(A)]
    F,

    [Requires<TestEnumAlternativeTypes>(true, TestEnumAlternativeTypes.Z, TestEnumAlternativeTypes.Y)]
    G
}

/// <summary>
/// Alternative test enum for cross-enum relationship testing.
/// </summary>
public enum TestEnumAlternativeTypes
{
    [Requires<TestEnumAlternativeTypes>(X)]
    Z,

    Y,
    X,
}

/// <summary>
/// Derived attribute for testing attribute inheritance.
/// </summary>
/// <param name="includes">The enum values to include</param>
public class DerivedIncludesAttribute(params TestEnumTypes[] includes) : IncludesAttribute<TestEnumTypes>(includes)
{
}

/// <summary>
/// Test flags enum for flag operation testing.
/// </summary>
[Flags]
public enum TestFlagsEnum
{
    None = 0,
    First = 1 << 0,
    Second = 1 << 1,
    Third = 1 << 2
}
