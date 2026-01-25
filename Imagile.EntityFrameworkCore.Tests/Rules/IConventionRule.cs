using Imagile.EntityFrameworkCore.Tests.Configuration;
using Microsoft.EntityFrameworkCore;

namespace Imagile.EntityFrameworkCore.Tests.Rules;

/// <summary>
/// Interface for convention rules that validate DbContext configurations.
/// </summary>
public interface IConventionRule
{
    /// <summary>
    /// Gets the name of the rule.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Validates the rule against the provided DbContexts.
    /// </summary>
    /// <param name="contexts">The DbContexts to validate.</param>
    /// <param name="options">The convention test options containing exclusions.</param>
    /// <returns>A collection of convention violations found.</returns>
    IEnumerable<ConventionViolation> Validate(
        IEnumerable<DbContext> contexts,
        ConventionTestOptions options);
}
