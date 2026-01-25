# Codebase Concerns

**Analysis Date:** 2026-01-25

## Fragile Areas

### Rule Implementation Duplication

**Files:** `Imagile.EntityFrameworkCore.Tests/Rules/*.cs`

**Why fragile:** Each rule implements similar iteration patterns over contexts and entities with nearly identical boilerplate:
- Loop through contexts
- Get contextName
- Loop through entityType
- Check exclusions
- Build violations list

Pattern duplicates across 12 rule files. Changes to exclusion logic require updates in multiple places.

**Safe modification:** Extract common traversal logic into a base class or shared method:
```csharp
protected IEnumerable<ConventionViolation> ValidateProperties(
    IEnumerable<DbContext> contexts,
    ConventionTestOptions options,
    Func<IProperty, bool> shouldCheck,
    Func<IProperty, bool> isViolation)
```

**Test coverage:** All rules have test methods in `DbContextConventionTests.cs` but individual rule unit tests are limited to integration-style testing.

### Nullable Reference Type Initialization

**Files:** `Imagile.EntityFrameworkCore.Tests/Infrastructure/InMemoryDatabaseTest.cs`

**Why fragile:** Properties are marked `= null!;` during declaration (lines 19, 24):
```csharp
protected TContext Context { get; private set; } = null!;
protected DbContextOptions<TContext> ContextOptions { get; private set; } = null!;
```

These properties are only initialized in `InitializeAsync()`. If a test calls methods before async initialization completes, runtime null reference exceptions occur.

**Safe modification:** Consider using lazy initialization pattern or ensuring callers understand IAsyncLifetime contract explicitly in documentation.

**Test coverage:** Integration tests in sample project demonstrate proper usage, but no explicit tests validate initialization contract.

### String-based Exclusion Matching

**Files:** `Imagile.EntityFrameworkCore.Tests/Configuration/ConventionTestOptions.cs` (lines 18-54)

**Why fragile:** Exclusions use string entity/property names with direct string equality checks. No validation that exclusion keys actually exist in the DbContext model. Typos in exclusion configuration silently pass.

Example:
```csharp
builder.ExcludeProperty("Employee", "Deparment")  // Typo: "Deparment" not "Department"
// Silently ignored, rule still runs on actual property
```

**Safe modification:** Add optional validation mode that validates exclusion keys exist in model on build.

**Test coverage:** Configuration is tested in sample project but no tests for invalid exclusion keys.

## Test Coverage Gaps

### Owned Entities and Complex Keys

**Files:** `Imagile.EntityFrameworkCore.Tests/Rules/PrimaryKeysMustBeIntsRule.cs` (lines 26-28, 46-48)
`Imagile.EntityFrameworkCore.Tests/Rules/PrimaryKeyMustBeEntityNameIdRule.cs` (lines 27-29, 47-49)

**What's not tested:** Rules skip owned entities and composite primary keys without testing these edge cases:
- Owned entity with int primary key
- Composite keys where some properties are int, some are not
- Owned entities within owned entities (nested)

**Files:** Sample data has no owned entities or composite keys

**Risk:** Rules silently skip scenarios that may violate conventions, giving false sense of compliance.

**Priority:** Medium - affects completeness but not correctness of implemented rules.

### Table Naming Case Sensitivity

**Files:** `Imagile.EntityFrameworkCore.Tests/Rules/TableNamesMustBePluralRule.cs` (line 42)

**What's not tested:** Pluralization comparison uses `StringComparison.OrdinalIgnoreCase` but Humanizer.Pluralize() may produce differently-cased results on edge cases:

```csharp
var pluralTableName = tableName.Pluralize(inputIsKnownToBeSingular: false);
if (!string.Equals(tableName, pluralTableName, StringComparison.OrdinalIgnoreCase))
```

Sample only tests standard English plurals (Users, BlogPosts, Comments). No tests for:
- Irregular plurals (Person → People)
- Acronyms (API → APIs)
- Already-plural inputs

**Risk:** Humanizer library may change pluralization logic in future versions causing test failures.

**Priority:** Low - Humanizer is stable, but document the dependency.

### Nullable DateTime Handling

**Files:** `Imagile.EntityFrameworkCore.Tests/Rules/DateTimesMustEndWithDateRule.cs` (lines 38-43)

**What's not tested:** Rule checks nullable DateTime properties but sample entity `User.ModifiedDate` (nullable DateTime) is not configured in `OnModelCreating`. Unknown if convention validation actually runs on nullable DateTime properties properly.

**Risk:** Nullable DateTimes may not trigger rule violations if not explicitly configured.

**Priority:** Medium - affects rule correctness.

### Enum Detection Edge Cases

**Files:** `Imagile.EntityFrameworkCore.Tests/Rules/EnumsMustEndWithTypeRule.cs` (lines 37-39)

**What's not tested:** Enum detection using reflection:
```csharp
var isEnum = clrType.IsEnum ||
             (Nullable.GetUnderlyingType(clrType)?.IsEnum ?? false);
```

No tests for:
- Enums stored as string or different backing types
- Generic enum properties
- Flagged enums

**Priority:** Low - standard enum detection covers most cases.

## Known Issues

### Connection Not Closed in Sample Test

**Files:** `Samples/SampleApp.Tests/DatabaseConventionTests.cs` (lines 17-52)

**Symptoms:** SqliteConnection is created in `CreateContexts()` but not properly tracked. The `DisposeAsync()` override disposes the connection, but the connection reference is only stored as private field, not returned with context.

**Files affected:** Connection opened at line 23 is field in test class but contexts are created with that connection. If base class `DbContextConventionTests.DisposeAsync()` runs before derived class override, connection may not be disposed properly.

**Workaround:** Current code works because derived class override calls `base.DisposeAsync()` first, but this is fragile and depends on call order.

**Priority:** Medium - potential resource leak if inheritance pattern changes.

## Performance Bottlenecks

### Rule Validation Performance

**Files:** `Imagile.EntityFrameworkCore.Tests/Rules/*.cs`

**Problem:** Each rule iterates through all contexts, all entities, and all properties independently. For DbContexts with many entities and properties, rules perform redundant iteration.

Current approach: 12 rules × N contexts × M entities × P properties

**Cause:** No caching of DbContext model inspection results. Each rule re-validates the same metadata.

**Improvement path:** Cache entity/property metadata once in `DbContextConventionTests` initialization, pass cached results to rules. Could reduce iterations by 80%+ for complex models.

**Priority:** Low - performance acceptable for typical test-time operation, but will degrade with very large models (100+ entities).

## Dependencies at Risk

### Humanizer.Core 3.0.1 Dependency

**Risk:** Only dependency outside Microsoft.EntityFrameworkCore ecosystem. Used exclusively for pluralization in `TableNamesMustBePluralRule`.

**Impact:** Major version changes in Humanizer could change pluralization behavior, causing existing tests to fail unexpectedly.

**Migration plan:** If Humanizer becomes unmaintained or problematic, replace with simple hardcoded rules:
- If last character is 's', check if it's already plural
- Use `EndsWith()` checks for common patterns (Users, Posts, Comments)

**Priority:** Low - Humanizer is well-maintained, but consider vendoring pluralization logic long-term.

## Scaling Limits

### Multiple DbContext Support

**Current capacity:** Designed to handle multiple DbContexts, tested with 2 contexts in sample.

**Limit:** `DbContextConventionTests._contexts` is `List<DbContext>`. Memory usage grows linearly with:
- Number of DbContexts
- Size of each context's model
- Number of rule validation iterations

No pagination or streaming of context validation.

**Scaling path:** For 50+ contexts, consider:
1. Process contexts in batches
2. Stream rule results instead of materializing all violations
3. Add parallel rule execution (currently sequential)

**Priority:** Low - typical test scenarios use 1-3 contexts.

## Security Considerations

### No Input Validation on Exclusions

**Risk:** String-based exclusion keys accept any string with no validation. Malicious exclusion configurations could:
- Exclude all entities via catch-all patterns (not currently supported, but could be added)
- Bypass important convention rules through configuration

**Current mitigation:** Exclusions are set only in test code by developers, not from external input.

**Recommendations:**
1. Document that exclusion configuration is trusted input only
2. Consider whitelist validation if exclusions come from configuration files in future
3. No changes needed for current internal use

**Priority:** Very Low - not a real security risk for internal testing library.

## Missing Critical Features

### No Rule Severity Levels

**Problem:** All rule violations treated equally in assertion. No ability to:
- Warn vs. fail
- Skip certain rules in certain contexts
- Have different rule sets for different projects

**Blocks:** Can't gradually adopt conventions or enforce some rules at team level while others at project level.

**Priority:** Low - feature request for future enhancement, not blocking current functionality.

### No Violation Explanation or Guidance

**Problem:** Violations report only entity/property name, not what the convention requires or how to fix it.

Example violation message:
```
"because all entities should comply with the rule: String properties must have max length"
```

Doesn't tell developer:
- Which property lacks max length
- What max length to use
- EF Core API to configure it

**Priority:** Medium - developer experience issue. Consider adding suggested fix to violations.

### Limited Exclusion Granularity

**Problem:** Can exclude entity from rule, or property from rule, but not:
- Exclude based on property type
- Exclude based on property attribute presence
- Exclude composite keys while allowing single keys
- Conditional exclusions

**Priority:** Low - current exclusion model sufficient for most use cases.

## Architectural Issues

### Test Methods Cannot Be Overridden

**Files:** `Imagile.EntityFrameworkCore.Tests/DbContextConventionTests.cs` (lines 58-140)

**Issue:** Test methods (`PrimaryKeysMustBeInts`, `StringPropertiesMustHaveMaxLength`, etc.) are not virtual. Derived test classes cannot:
- Skip certain rule tests
- Replace test logic
- Add custom test methods that follow same pattern

All 12 test methods must run every time, no way to opt-out of specific rules at test class level.

**Impact:** Can only exclude entities/properties, not disable entire rules. For mixed-convention codebases, forces developers to exclude many entities rather than skip the rule.

**Fix approach:** Make test methods virtual or extractable into separate test fixtures that can be composed.

**Priority:** Medium - limits flexibility but not functionality. Workaround: configure exclusions broadly.

