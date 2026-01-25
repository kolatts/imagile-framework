# Domain Pitfalls: .NET Framework Library Development

**Domain:** Multi-package .NET framework library
**Target Framework:** .NET 10
**Distribution:** NuGet packages
**Researched:** 2026-01-25
**Overall confidence:** HIGH

## Executive Summary

.NET framework library development, especially multi-package libraries, presents numerous pitfalls related to dependency management, versioning, API design, and modern .NET requirements (AOT, trimming). The most critical mistakes involve dependency mismanagement (viral dependencies, diamond conflicts), breaking changes (assembly identity changes), and failure to design for modern deployment targets (Native AOT, trimming). EF Core audit logging implementations commonly suffer from transaction scope confusion and performance issues, while Blazor integrations face architecture-specific challenges (Server vs WASM). Attribute systems require careful design to avoid reflection pitfalls and maintain AOT compatibility.

---

## Critical Pitfalls

Mistakes that cause rewrites, major refactoring, or fundamental architecture changes.

### Pitfall 1: Viral Dependency Propagation

**What goes wrong:**
Adding dependencies to a core library forces all consumers to inherit those dependencies, causing version conflicts, bloated binaries, and diamond dependency problems. A single careless `<PackageReference>` can ripple through consumer projects, causing runtime crashes and version conflicts.

**Why it happens:**
- Not understanding that consumers inherit ALL your dependencies
- Adding convenience dependencies to core packages
- Not using `PrivateAssets="All"` for development-only packages
- Publishing shared source packages to public NuGet feeds

**Consequences:**
- Diamond dependency conflicts in consumer applications
- Inability for consumers to upgrade dependencies independently
- Bloated package sizes
- Runtime assembly binding failures
- Breaking changes cascade across entire ecosystem

**Prevention:**
1. **Keep core packages dependency-free** - Core library MUST have zero dependencies
2. **Use PrivateAssets for build-time dependencies:**
   ```xml
   <!-- Analyzers, source generators - not transitive -->
   <PackageReference Include="Analyzer" Version="1.0" PrivateAssets="All" />
   ```
3. **Use shared source packages for internal utilities:**
   ```xml
   <PackageReference Include="Internal.Shared" Version="1.0" PrivateAssets="All" />
   ```
4. **Never expose shared source types in public APIs** - They're compiled separately per assembly
5. **Review dependency graph** before each release - Use `dotnet list package --include-transitive`
6. **Document minimum versions without upper limits:**
   ```xml
   <!-- GOOD: Accepts 1.0 and above -->
   <PackageReference Include="Library" Version="1.0" />

   <!-- BAD: Exact version causes conflicts -->
   <PackageReference Include="Library" Version="[1.0]" />

   <!-- BAD: Upper limit guarantees build failures -->
   <PackageReference Include="Library" Version="[1.0,2.0)" />
   ```

**Detection:**
- Consumers report "assembly binding redirect" errors
- Multiple versions of same package in consumer dependency trees
- NuGet restore warnings about downgraded packages (NU1605, NU1608)
- Consumer complaints about package size

**Phase mapping:**
- Phase 1 (Core foundation): Establish zero-dependency policy
- Phase 2 (Each new package): Validate dependency necessity before adding

**Sources:**
- [Dependencies and .NET libraries - Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/standard/library-guidance/dependencies)
- [Best Practices for Managing Shared Libraries in .NET Applications at Scale - InfoQ](https://www.infoq.com/articles/shared-libraries-dotnet-enterprise/)

---

### Pitfall 2: Assembly Identity Changes (Strong Naming)

**What goes wrong:**
Changing assembly name, strong naming key, or assembly version breaks all compiled code that references the library. This is a binary breaking change that requires recompilation of all consumers.

**Why it happens:**
- Not understanding assembly versioning strategy upfront
- Adding/removing strong naming after initial release
- Changing strong name key for security reasons
- Incrementing AssemblyVersion for minor releases
- Not understanding the difference between AssemblyVersion, FileVersion, and PackageVersion

**Consequences:**
- All consuming assemblies break at runtime
- Requires synchronized updates across entire ecosystem
- Can't gradually roll out changes
- GAC conflicts in enterprise environments
- Binding redirects proliferate in consumer app.config files

**Prevention:**
1. **Decide on strong naming BEFORE first release** - Can't change without breaking
2. **Only increment AssemblyVersion for MAJOR versions:**
   ```xml
   <!-- Library 1.0, 1.0.1, 1.1 all use same AssemblyVersion -->
   <AssemblyVersion>1.0.0.0</AssemblyVersion>

   <!-- Library 2.0 gets new AssemblyVersion -->
   <AssemblyVersion>2.0.0.0</AssemblyVersion>
   ```
3. **Keep AssemblyVersion and PackageVersion major versions synchronized**
4. **NEVER change assembly name** - This is part of the assembly identity
5. **NEVER change strong naming key** - Existing compiled code won't accept new key
6. **For modern .NET (Core/.NET 5+), strong naming provides NO benefits:**
   - Runtime doesn't validate signatures
   - Doesn't use strong name for binding
   - Only needed for compatibility with legacy .NET Framework code
7. **If you must strong name, understand viral nature:**
   - Strong-named assemblies can ONLY reference other strong-named assemblies
   - Forces entire dependency tree to be strong-named
   - Open source libraries can't be forked and drop-in replaced

**Detection:**
- FileLoadException: "Could not load file or assembly"
- MissingMethodException or TypeLoadException at runtime
- Consumers forced to add binding redirects
- Consumer projects fail to build after upgrade

**Phase mapping:**
- Phase 0 (Pre-implementation): Document versioning strategy
- Phase 1 (Core library): Set AssemblyVersion to 1.0.0.0 and NEVER change it

**Sources:**
- [Strong naming and .NET libraries - Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/standard/library-guidance/strong-naming)
- [Breaking changes and .NET libraries - Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/standard/library-guidance/breaking-changes)
- [Versioning and .NET libraries - Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/standard/library-guidance/versioning)

---

### Pitfall 3: Native AOT and Trimming Incompatibility

**What goes wrong:**
Library uses reflection, dynamic code generation, or unmarked APIs, causing failures when consumers publish with Native AOT or trimming enabled. As of 2026, Native AOT is "table stakes" for modern .NET applications.

**Why it happens:**
- Using reflection without trim/AOT annotations
- Walking type graphs dynamically (serialization patterns)
- Using System.Reflection.Emit
- Not testing with PublishTrimmed and PublishAot
- Assuming all .NET features work in AOT context

**Consequences:**
- Library unusable in Native AOT applications
- Runtime crashes in trimmed applications
- Missing types/methods at runtime (no compile-time error)
- Consumer applications 70% slower startup, 2x larger binaries
- Library excluded from modern cloud-native architectures

**Prevention:**
1. **Enable AOT/trim analysis in library project:**
   ```xml
   <IsAotCompatible>true</IsAotCompatible>
   <IsTrimmable>true</IsTrimmable>
   ```
2. **Mark incompatible APIs with attributes:**
   ```csharp
   // For trimming-incompatible APIs
   [RequiresUnreferencedCode("Uses reflection to walk type graphs")]
   public void DynamicMethod(Type type) { }

   // For AOT-incompatible APIs
   [RequiresDynamicCode("Uses Reflection.Emit")]
   public void GenerateCode() { }
   ```
3. **Use DynamicallyAccessedMembersAttribute for reflection:**
   ```csharp
   public void Process([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type)
   {
       // Tells trimmer to preserve public properties
   }
   ```
4. **Provide AOT-compatible alternatives:**
   ```csharp
   // Reflection-based (backward compat)
   [RequiresUnreferencedCode("Use CreateValidator<T>() instead")]
   public Validator CreateValidator(Type type) { }

   // Source generator or compile-time alternative
   public Validator CreateValidator<T>() where T : class { }
   ```
5. **Use source generators instead of runtime reflection** where possible
6. **Test with PublishAot and PublishTrimmed enabled**
7. **Avoid walking type graphs** - This is the #1 AOT incompatibility

**Detection:**
- ILC (AOT compiler) warnings during consumer publish
- IL2026, IL2087 warnings (trimming)
- IL3050, IL3051 warnings (Native AOT)
- Runtime MissingMethodException or TypeLoadException in trimmed apps
- Consumer complaints about "doesn't work with AOT"

**Phase mapping:**
- Phase 1 (Core): Enable trim/AOT analysis from day one
- Phase 2 (Attributes): Test attribute reflection with AOT
- Phase 3 (EF Core integration): Validate DbContext with trimming
- Phase 4 (Blazor WASM): Test with AOT (WASM benefits most)

**Sources:**
- [How to make libraries compatible with native AOT - .NET Blog](https://devblogs.microsoft.com/dotnet/creating-aot-compatible-libraries/)
- [Surviving Native AOT: The Reflection Migration Guide - Medium](https://medium.com/@dikhyantkrishnadalai/surviving-native-aot-the-reflection-migration-guide-every-net-architect-needs-fa3760fbb41b)

---

### Pitfall 4: EF Core Audit Logging Transaction Scope Confusion

**What goes wrong:**
Audit log interceptors write audit records in separate transactions, causing inconsistent audit trails when primary operation fails or rolls back. Audit records exist for operations that never completed.

**Why it happens:**
- Writing audit records directly from interceptor
- Not understanding SaveChangesAsync creates implicit transactions
- Each SaveChangesAsync call is a separate transaction boundary
- Using TransactionScope without AsyncFlowOption.Enabled
- Not handling ExecuteUpdate/ExecuteDelete (bypass SaveChanges)

**Consequences:**
- Orphaned audit records for failed operations
- Compliance violations (GDPR, HIPAA, SOX)
- Audit trail inconsistency
- Debugging nightmares (logs show operations that didn't happen)
- Legal risk if audit trail is inaccurate

**Prevention:**
1. **Buffer audit events in interceptor, write within same transaction:**
   ```csharp
   // WRONG: Direct database write from interceptor
   public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(...)
   {
       await auditDbContext.SaveChangesAsync(); // SEPARATE TRANSACTION!
       return result;
   }

   // CORRECT: Buffer events, write in same transaction
   public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(...)
   {
       var auditEvents = ExtractAuditEvents(context);
       context.Set<AuditLog>().AddRange(auditEvents); // Same transaction
       return result;
   }
   ```
2. **Handle ExecuteUpdate/ExecuteDelete explicitly:**
   ```csharp
   // These bypass SaveChanges - need explicit transaction
   await using var transaction = await dbContext.Database.BeginTransactionAsync();
   await dbContext.Products.Where(p => p.Price > 100).ExecuteUpdateAsync(...);
   await dbContext.AuditLogs.AddAsync(new AuditLog { ... });
   await dbContext.SaveChangesAsync();
   await transaction.CommitAsync();
   ```
3. **Use Database.BeginTransactionAsync() for complex scenarios**
4. **Enable TransactionScope async flow:**
   ```csharp
   using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
   await dbContext.SaveChangesAsync();
   scope.Complete();
   ```
5. **Interceptor responsibility is narrow: inspect, extract, buffer** - NOT write
6. **Consider separate audit store with eventual consistency** if audit volume is high
7. **Avoid over-indexing audit tables** - They're write-heavy

**Detection:**
- Audit records for operations that show as failed in application logs
- Audit record count != entity change count
- Integration test failures when asserting audit consistency
- Compliance audit findings
- Orphaned audit records after transaction rollback

**Phase mapping:**
- Phase X (EF Core Audit): Research transaction strategy BEFORE implementation
- Phase X (EF Core Audit): Integration tests must cover rollback scenarios
- Phase X (EF Core Audit): Load testing audit write performance

**Sources:**
- [Comprehensive Guide to Implementing Audit Logging in .NET with EF Core Interceptors - DEV Community](https://dev.to/hootanht/comprehensive-guide-to-implementing-audit-logging-in-net-with-ef-core-interceptors-1e83)
- [EF Core Transaction Guide - Devart](https://www.devart.com/dotconnect/ef-core-transactions.html)
- [3 Essential Techniques for Managing Transactions in EF Core - Elmah.io](https://blog.elmah.io/3-essential-techniques-for-managing-transactions-in-ef-core/)

---

## Moderate Pitfalls

Mistakes that cause delays, technical debt, or significant rework.

### Pitfall 5: Over-Exposing Public API Surface

**What goes wrong:**
Marking too many types as public creates a massive API surface that you can't change without breaking consumers. "Everything is public" means you can NEVER refactor internal details.

**Why it happens:**
- Default `public` during prototyping
- Not understanding that public = forever commitment
- Thinking "internal" means "hard to test"
- Not using InternalsVisibleTo for test assemblies

**Consequences:**
- Locked into poor design decisions
- Can't refactor without major version bump
- Consumers depend on implementation details
- Technical debt accumulates
- Every change becomes a breaking change

**Prevention:**
1. **Default to internal, promote to public only when needed**
2. **Use InternalsVisibleTo for test access:**
   ```csharp
   // AssemblyInfo.cs or .csproj
   [assembly: InternalsVisibleTo("YourLibrary.Tests")]
   ```
3. **For strong-named assemblies, include public key:**
   ```csharp
   [assembly: InternalsVisibleTo("YourLibrary.Tests, PublicKey=...")]
   ```
4. **It's easy to make private→public, impossible to make public→private**
5. **Review public API surface before each release**
6. **Use API analyzer (PublicApiAnalyzers) to track public API changes**

**Detection:**
- Large API surface but small documented feature set
- Consumer questions about "should I use ClassA or ClassB for X?"
- Difficulty refactoring without breaking changes
- Many types marked public but never used by consumers

**Phase mapping:**
- Phase 1 onwards: Code review checklist includes "Is public necessary?"
- Before each release: Run API analyzer, review public additions

**Sources:**
- [Top 15 Mistakes .NET Developers Make - Anton Dev Tips](https://antondevtips.com/blog/top-15-mistakes-dotnet-developers-make-how-to-avoid-common-pitfalls)
- [Friend assemblies - .NET - Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/standard/assembly/friend)

---

### Pitfall 6: Central Package Management (CPM) Misconfiguration

**What goes wrong:**
Directory.Packages.props in wrong location, Version attributes left in .csproj, PrivateAssets/IncludeAssets in wrong file, causing NU1008 errors and version inconsistencies across packages.

**Why it happens:**
- Not understanding CPM file structure
- Leaving Version in PackageReference after CPM migration
- Placing Directory.Packages.props in project folder instead of solution root
- Not cleaning bin/obj after migration
- Setting PrivateAssets on PackageVersion instead of PackageReference

**Consequences:**
- NU1008: Projects that use central package versioning should not define version on PackageReference
- Version inconsistency across multi-package library
- Build failures on CI/CD
- Dependency drift between packages
- Diamond dependency issues multiply

**Prevention:**
1. **Directory.Packages.props location:**
   ```
   YourSolution/
   ├── Directory.Packages.props       ← HERE (solution root)
   ├── YourLibrary.Core/
   │   └── YourLibrary.Core.csproj
   └── YourLibrary.EfCore/
       └── YourLibrary.EfCore.csproj
   ```
2. **Enable CPM in Directory.Packages.props:**
   ```xml
   <Project>
     <PropertyGroup>
       <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
     </PropertyGroup>
     <ItemGroup>
       <PackageVersion Include="Microsoft.EntityFrameworkCore" Version="10.0.0" />
     </ItemGroup>
   </Project>
   ```
3. **Remove Version from .csproj PackageReference:**
   ```xml
   <!-- WRONG: Version in PackageReference with CPM enabled -->
   <PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.0.0" />

   <!-- CORRECT: No Version attribute -->
   <PackageReference Include="Microsoft.EntityFrameworkCore" />
   ```
4. **IncludeAssets/PrivateAssets go in PackageReference, NOT PackageVersion:**
   ```xml
   <!-- Directory.Packages.props -->
   <PackageVersion Include="Analyzer" Version="1.0.0" />

   <!-- YourProject.csproj -->
   <PackageReference Include="Analyzer" PrivateAssets="All" />
   ```
5. **Clean bin/obj after CPM migration:** `dotnet clean && dotnet restore`

**Detection:**
- NU1008 warnings/errors during build
- Different package versions in different projects
- Build works locally, fails on CI
- NuGet restore errors

**Phase mapping:**
- Phase 0 (Setup): Configure CPM from the start if multi-package
- Phase 1: Validate CPM working before adding more packages

**Sources:**
- [Implementing NuGet Central Package Management (CPM) in a .NET Solution - Medium](https://sachidevop.medium.com/implementing-nuget-central-package-management-cpm-in-a-net-solution-using-visual-studio-2026-c93f207edcb6)
- [Central Package Management (CPM) - NuGet - Microsoft Learn](https://learn.microsoft.com/en-us/nuget/consume-packages/central-package-management)
- [NuGet PackageReference in project files - Microsoft Learn](https://learn.microsoft.com/en-us/nuget/consume-packages/package-references-in-project-files)

---

### Pitfall 7: Attribute System Design Without AOT Consideration

**What goes wrong:**
Custom attributes designed with mutable properties, reflection-based validation, or wrong AttributeUsage, causing AOT incompatibility, unexpected behavior, or misuse.

**Why it happens:**
- Not following Microsoft attribute design guidelines
- Making attribute properties publicly settable
- Forgetting AttributeUsageAttribute
- Overloading constructors (confuses required vs optional)
- Not sealing attribute classes
- Using reflection-heavy validation in attribute constructors

**Consequences:**
- Attributes applied to wrong targets (methods, classes, etc.)
- Cached attribute instances have modified values
- Slower attribute lookup (unsealed classes)
- AOT/trimming failures
- Confusing API (what's required vs optional?)

**Prevention:**
1. **Always apply AttributeUsage:**
   ```csharp
   // WRONG: No AttributeUsage = can be applied anywhere
   public class AuditAttribute : Attribute { }

   // CORRECT: Explicit targets
   [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method,
                   AllowMultiple = false,
                   Inherited = true)]
   public sealed class AuditAttribute : Attribute { }
   ```
2. **Seal attribute classes** for performance:
   ```csharp
   public sealed class AuditAttribute : Attribute { }
   ```
3. **Required arguments = constructor parameters (read-only properties):**
   ```csharp
   public sealed class AuditAttribute : Attribute
   {
       // Required: constructor parameter, no setter
       public string Operation { get; }

       // Optional: property with setter
       public string Category { get; set; }

       public AuditAttribute(string operation) // Required
       {
           Operation = operation;
       }
   }
   ```
4. **Do NOT overload constructors** - Confuses required vs optional
5. **Do NOT make attribute properties mutable after construction** (except optional ones)
6. **Design for AOT compatibility:**
   ```csharp
   // Mark if attribute requires reflection-based validation
   [RequiresUnreferencedCode("Validates using reflection")]
   public sealed class ValidateAttribute : Attribute
   {
       // Provide AOT-friendly alternative if possible
   }
   ```
7. **Name with "Attribute" suffix:** `AuditAttribute`, not `Audit`

**Detection:**
- Attribute applied to unintended targets
- Unexpected null values in attribute properties
- AOT warnings (IL3050, IL3051) when using attributes
- Slow attribute retrieval in hot paths
- Consumer confusion about which constructor to use

**Phase mapping:**
- Phase X (Attribute system): Design attributes following guidelines from start
- Phase X (Attribute system): Test attribute usage with trimming/AOT enabled

**Sources:**
- [Attributes (.NET Framework design guidelines) - Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/attributes)
- [Writing Custom Attributes - .NET - Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/standard/attributes/writing-custom-attributes)

---

### Pitfall 8: Blazor Server vs WASM Library Design Assumptions

**What goes wrong:**
Library designed for Blazor Server fails in WASM (or vice versa) due to wrong assumptions about code execution location, available services, or sandboxing constraints.

**Why it happens:**
- Assuming server-side execution context in WASM
- Using NuGet packages incompatible with browser sandbox
- Not understanding that WASM code runs client-side
- Using HttpContext in library (Server-only)
- Expecting full .NET runtime (WASM is limited)

**Consequences:**
- Library works in Server, fails in WASM
- Runtime errors in browser (sandbox violations)
- Missing DI services in one hosting model
- Application Insights telemetry fails in WASM
- Performance characteristics differ wildly

**Prevention:**
1. **Design for BOTH hosting models** if claiming Blazor support:
   ```csharp
   // WRONG: HttpContext only exists in Server
   public class MyService
   {
       public MyService(IHttpContextAccessor accessor) { } // Fails in WASM
   }

   // CORRECT: Use abstractions
   public class MyService
   {
       public MyService(IUserContext userContext) { } // Custom abstraction
   }
   ```
2. **Understand execution differences:**
   | Aspect | Blazor Server | Blazor WASM |
   |--------|--------------|-------------|
   | Execution | Server-side | Browser (client-side) |
   | .NET Runtime | Full .NET | Limited (WebAssembly) |
   | Package compat | All NuGet packages | .NET Standard 2.0 only |
   | Connection | SignalR (WebSocket) | HTTP requests |
   | HttpContext | Available | Not available |
   | File system | Available | Sandboxed/virtualized |
3. **Application Insights integration differs:**
   - **Server:** Standard ASP.NET Core App Insights
   - **WASM:** Requires JavaScript interop or community package (BlazorApplicationInsights)
   - Server tracks server-side telemetry only (not user flows)
   - WASM needs client-side telemetry
4. **Test in both hosting models** if supporting both
5. **Document hosting model requirements clearly**
6. **Use conditional compilation for hosting-specific code:**
   ```csharp
   #if BLAZOR_WASM
       // WASM-specific implementation
   #else
       // Server-specific implementation
   #endif
   ```

**Detection:**
- Library works in Server, fails in WASM (or vice versa)
- "Service not registered" errors in one hosting model
- Sandbox violation errors in browser console
- Missing telemetry in one hosting model
- Package restore warnings about incompatible frameworks

**Phase mapping:**
- Phase X (Blazor telemetry): Research hosting model compatibility BEFORE implementation
- Phase X (Blazor telemetry): Test in both Server and WASM projects

**Sources:**
- [Blazor Server vs. WebAssembly: Which Works Best For You? - Gap Velocity](https://www.gapvelocity.ai/blog/blazor-server-vs-webassembly)
- [GitHub - Application Insights and Blazor Server Issue](https://github.com/dotnet/AspNetCore.Docs/issues/30546)
- [BlazorApplicationInsights - GitHub](https://github.com/IvanJosipovic/BlazorApplicationInsights)

---

### Pitfall 9: Versioning Strategy Inconsistency

**What goes wrong:**
AssemblyVersion, FileVersion, and PackageVersion get out of sync. AssemblyVersion incremented for patch releases causes binding breaks. Not following SemVer causes consumer upgrade chaos.

**Why it happens:**
- Not understanding the four different version numbers
- Incrementing AssemblyVersion on every build
- Not following SemVer conventions
- Manual version management without tooling
- Build server auto-incrementing wrong version

**Consequences:**
- Binary breaking changes in patch releases
- Consumer confusion about compatibility
- Assembly binding redirect hell
- Can't have multiple versions in GAC
- Unclear upgrade path for consumers

**Prevention:**
1. **Understand the four versions:**
   | Version | Purpose | When to Change | Format |
   |---------|---------|----------------|--------|
   | PackageVersion | NuGet, visible to developers | Every release | SemVer 2.0 |
   | AssemblyVersion | CLR binding (strong-named only) | MAJOR only | 1.0.0.0 |
   | FileVersion | Windows properties | Every build | Major.Minor.Patch.BuildNum |
   | InformationalVersion | Metadata | Auto (Source Link) | 1.0.0-beta1+sha |

2. **Configure in .csproj:**
   ```xml
   <PropertyGroup>
     <!-- SemVer for NuGet -->
     <PackageVersion>1.2.3-beta1</PackageVersion>

     <!-- Only MAJOR version (stay on 1.0.0.0 for all 1.x releases) -->
     <AssemblyVersion>1.0.0.0</AssemblyVersion>

     <!-- Include build number from CI -->
     <FileVersion>1.2.3.$(BuildNumber)</FileVersion>

     <!-- Let Source Link auto-generate with commit hash -->
     <InformationalVersion>$(PackageVersion)+$(CommitHash)</InformationalVersion>
   </PropertyGroup>
   ```

3. **Follow SemVer 2.0.0 strictly:**
   - **MAJOR:** Breaking changes (1.0.0 → 2.0.0)
   - **MINOR:** New features, backward compatible (1.0.0 → 1.1.0)
   - **PATCH:** Bug fixes only (1.0.0 → 1.0.1)
   - **Pre-release:** -alpha1, -beta2, -rc3

4. **Keep AssemblyVersion and PackageVersion major version in sync:**
   - Package 1.x.x → Assembly 1.0.0.0
   - Package 2.x.x → Assembly 2.0.0.0

5. **Use MinVer or GitVersion for automatic versioning** from Git tags

6. **Document versioning strategy in CONTRIBUTING.md**

**Detection:**
- Consumer complaints about "worked in 1.0.5, broke in 1.0.6"
- Assembly binding failures after minor version upgrade
- Unable to install multiple versions side-by-side
- SemVer violations flagged by NuGet analyzers

**Phase mapping:**
- Phase 0 (Pre-implementation): Document versioning strategy
- Phase 1 (First package): Configure all four version numbers correctly
- Before each release: Validate version increment follows SemVer

**Sources:**
- [Versioning and .NET libraries - Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/standard/library-guidance/versioning)
- [Breaking changes and .NET libraries - Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/standard/library-guidance/breaking-changes)

---

### Pitfall 10: DbContext Lifetime Scope Mismanagement

**What goes wrong:**
DbContext registered with wrong lifetime (Singleton instead of Scoped), injected into incompatible services (IHostedService), or multiple instances created per request, causing thread safety issues, stale data, or tracking conflicts.

**Why it happens:**
- Not understanding DI service lifetimes
- Trying to inject Scoped DbContext into Singleton service
- Creating multiple DbContext instances per HTTP request
- Wrapping DbContext in using() statements in web apps
- Blazor Server unique scoping (one scoped instance per user circuit)

**Consequences:**
- Thread safety violations (DbContext not thread-safe)
- Stale data (DbContext caches entities)
- Tracked entity conflicts across requests
- Memory leaks (DbContext not disposed)
- Runtime errors: "Cannot access a disposed object"
- Data corruption in high-concurrency scenarios

**Prevention:**
1. **Use Scoped lifetime for DbContext:**
   ```csharp
   // CORRECT: Scoped (default from AddDbContext)
   services.AddDbContext<AppDbContext>(options => ...);

   // WRONG: Singleton = thread safety nightmare
   services.AddSingleton<AppDbContext>(); // NEVER DO THIS
   ```

2. **For Singleton services (IHostedService), use IServiceScopeFactory:**
   ```csharp
   public class BackgroundService : IHostedService
   {
       private readonly IServiceScopeFactory _scopeFactory;

       public BackgroundService(IServiceScopeFactory scopeFactory)
       {
           _scopeFactory = scopeFactory;
       }

       public async Task DoWork()
       {
           using var scope = _scopeFactory.CreateScope();
           var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
           // Use dbContext
       }
   }
   ```

3. **For factory pattern, use IDbContextFactory:**
   ```csharp
   services.AddDbContextFactory<AppDbContext>(options => ...);

   public class MyService
   {
       private readonly IDbContextFactory<AppDbContext> _factory;

       public async Task DoWork()
       {
           await using var dbContext = await _factory.CreateDbContextAsync();
           // Use dbContext
       }
   }
   ```

4. **For Blazor Server, use IDbContextFactory** to avoid per-circuit lifetime:
   ```csharp
   // Blazor Server: One circuit = entire user session
   // Don't inject DbContext directly - use factory
   @inject IDbContextFactory<AppDbContext> DbFactory

   @code {
       protected override async Task OnInitializedAsync()
       {
           await using var context = await DbFactory.CreateDbContextAsync();
           // Use context
       }
   }
   ```

5. **DON'T wrap DbContext in using() in web apps:**
   ```csharp
   // WRONG: Creates multiple DbContext per request
   public class MyController
   {
       private readonly IServiceProvider _provider;

       public IActionResult Get()
       {
           using var db = _provider.GetService<AppDbContext>(); // BAD
           return Ok(db.Products.ToList());
       }
   }

   // CORRECT: Inject once, use throughout request
   public class MyController
   {
       private readonly AppDbContext _db;

       public MyController(AppDbContext db)
       {
           _db = db; // Disposed at end of request automatically
       }

       public IActionResult Get()
       {
           return Ok(_db.Products.ToList());
       }
   }
   ```

6. **One DbContext instance per web request** - The golden rule

**Detection:**
- InvalidOperationException: "A second operation was started on this context before a previous operation completed"
- ObjectDisposedException: "Cannot access a disposed object"
- Tracked entity conflicts: "The instance of entity type cannot be tracked"
- Stale data across requests
- Thread safety exceptions in concurrent scenarios

**Phase mapping:**
- Phase X (EF Core integration): Document DbContext lifetime requirements
- Phase X (EF Core integration): Provide IDbContextFactory-based examples
- Phase X (EF Core integration): Test with Blazor Server, IHostedService scenarios

**Sources:**
- [DbContext Lifetime, Configuration, and Initialization - EF Core - Microsoft Learn](https://learn.microsoft.com/en-us/ef/core/dbcontext-configuration/)
- [Avoid Wrapping DbContext in Using - Ardalis](https://ardalis.com/avoid-wrapping-dbcontext-in-using/)
- [When Your DbContext Has The Wrong Scope - Haacked](https://haacked.com/archive/2023/01/09/scoping-db-context-copy/)

---

## Minor Pitfalls

Mistakes that cause annoyance or minor issues but are easily fixable.

### Pitfall 11: XML Documentation Comments Incomplete or Malformed

**What goes wrong:**
Missing XML doc comments, malformed XML, language-specific keywords (null instead of langword), inconsistent punctuation, causing poor IntelliSense and documentation generation failures.

**Why it happens:**
- Not generating XML doc file in library project
- Treating XML docs as optional
- Copy-pasting descriptions without customizing
- Not understanding special tags (langword, paramref, etc.)
- Extension methods with new C# syntax (14 extension blocks)

**Consequences:**
- Poor consumer IntelliSense experience
- Documentation generation fails (SHFB, DocFX)
- Consumers don't know how to use APIs
- Professional appearance suffers
- Incorrect documentation worse than no documentation

**Prevention:**
1. **Enable XML documentation generation:**
   ```xml
   <PropertyGroup>
     <GenerateDocumentationFile>true</GenerateDocumentationFile>
     <NoWarn>$(NoWarn);1591</NoWarn> <!-- Suppress missing XML comment warnings -->
   </PropertyGroup>
   ```

2. **Document all public and protected members AT MINIMUM:**
   ```csharp
   /// <summary>
   /// Validates the specified entity and returns validation errors.
   /// </summary>
   /// <typeparam name="T">The type of entity to validate.</typeparam>
   /// <param name="entity">The entity instance to validate.</param>
   /// <returns>A collection of validation errors, or empty if valid.</returns>
   /// <exception cref="ArgumentNullException">Thrown when <paramref name="entity"/> is <see langword="null"/>.</exception>
   public IEnumerable<ValidationError> Validate<T>(T entity) where T : class
   {
       // Implementation
   }
   ```

3. **Use language-agnostic keywords:**
   ```csharp
   // WRONG: Language-specific
   /// <param name="value">The value, or null if not found.</param>

   // CORRECT: Language-agnostic
   /// <param name="value">The value, or <see langword="null"/> if not found.</param>
   ```

4. **Use proper XML doc tags:**
   - `<summary>` - Brief description (required)
   - `<remarks>` - Detailed explanation
   - `<param>` - Parameter description
   - `<returns>` - Return value description
   - `<exception>` - Exceptions thrown
   - `<example>` - Usage example with `<code>`
   - `<see cref="Type"/>` - Cross-references
   - `<paramref name="param"/>` - Reference parameter in description
   - `<see langword="null"/>` - Language keywords

5. **End sentences with periods** consistently

6. **For deprecated members, use ObsoleteAttribute** instead of documenting in XML:
   ```csharp
   [Obsolete("Use ValidateAsync<T>() instead. This method will be removed in v2.0.")]
   public void Validate<T>(T entity) { }
   ```

7. **Use `<inheritdoc/>` for inherited members** when appropriate:
   ```csharp
   /// <inheritdoc/>
   public override string ToString() { }
   ```

**Detection:**
- Warning CS1591: Missing XML comment for publicly visible type or member
- Documentation generation errors
- IntelliSense shows empty tooltip
- Consumer questions about API usage that should be documented

**Phase mapping:**
- Phase 1 onwards: Enable GenerateDocumentationFile from start
- Code review: Check XML docs present and well-formed
- Before release: Generate docs with DocFX/SHFB to validate

**Sources:**
- [Recommended XML documentation tags - C# reference - Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/xmldoc/recommended-tags)
- [Guidelines to Better XML Doc Comments and Documentation - Helixoft](https://www.helixoft.com/blog/guidelines-to-better-xml-doc-comments-and-documentation.html)

---

### Pitfall 12: NuGet Package Metadata Missing or Incorrect

**What goes wrong:**
Missing descriptions, poor tags, no license info, no project URL, incorrect package icon, causing poor discoverability on NuGet.org and unprofessional appearance.

**Why it happens:**
- Not configuring package metadata in .csproj
- Treating metadata as optional
- Not understanding metadata is IMMUTABLE after publish
- Poor tag selection

**Consequences:**
- Poor search ranking on NuGet.org
- Users can't find package
- Unprofessional appearance
- Can't edit metadata after publish (it's locked)
- Missing license info = users can't use it legally

**Prevention:**
1. **Configure all metadata in .csproj BEFORE first publish:**
   ```xml
   <PropertyGroup>
     <!-- REQUIRED -->
     <PackageId>YourCompany.YourLibrary.Core</PackageId>
     <PackageVersion>1.0.0</PackageVersion>
     <Description>Provides core functionality for auditing and validation in .NET applications.</Description>
     <Authors>Your Company</Authors>

     <!-- LICENSE (required for open source) -->
     <PackageLicenseExpression>MIT</PackageLicenseExpression>
     <!-- OR -->
     <PackageLicenseFile>LICENSE.txt</PackageLicenseFile>

     <!-- RECOMMENDED -->
     <PackageProjectUrl>https://github.com/yourcompany/yourlibrary</PackageProjectUrl>
     <RepositoryUrl>https://github.com/yourcompany/yourlibrary</RepositoryUrl>
     <RepositoryType>git</RepositoryType>
     <PackageTags>audit;logging;validation;entityframework;efcore</PackageTags>
     <PackageReadmeFile>README.md</PackageReadmeFile>
     <PackageIcon>icon.png</PackageIcon>

     <!-- Include README and icon in package -->
     <None Include="README.md" Pack="true" PackagePath="/" />
     <None Include="icon.png" Pack="true" PackagePath="/" />
   </PropertyGroup>
   ```

2. **Description guidelines:**
   - Short, clear sentence describing what it does
   - Not marketing fluff - functional description
   - "Provides audit logging for EF Core applications with minimal configuration"

3. **Tags guidelines:**
   - Include key terms related to package (max 4000 chars)
   - Use lowercase, semicolon-separated
   - Include technology names, use cases, problem domain
   - "audit;logging;efcore;entityframework;compliance;tracking"

4. **Remember: Once published, metadata is LOCKED** - You can't edit it

5. **Test package metadata BEFORE publishing to NuGet.org:**
   ```bash
   dotnet pack
   # Inspect .nupkg file (it's a .zip)
   ```

**Detection:**
- Empty or generic description on NuGet.org
- Missing license warning
- Low download count (poor discoverability)
- No package icon or README on NuGet.org

**Phase mapping:**
- Phase 1 (First package): Configure ALL metadata before first publish
- Phase 1: Review metadata with team before publishing
- Before each new package: Copy metadata template, customize

**Sources:**
- [Package authoring best practices - NuGet - Microsoft Learn](https://learn.microsoft.com/en-us/nuget/create-packages/package-authoring-best-practices)
- [NuGet NuSpec Fields Explained - Inedo Blog](https://blog.inedo.com/nuget/metadata-fields-required-and-avoid/)

---

### Pitfall 13: IAsyncDisposable Implementation Pitfalls

**What goes wrong:**
ConfigureAwait issues with `await using`, IoC container disposal failures, deadlocks from improper async disposal, missing DisposeAsyncCore for inheritance.

**Why it happens:**
- Not understanding interaction between `await using` and ConfigureAwait
- Combining asynchronous acquisition with `await using`
- Not implementing both IAsyncDisposable and IDisposable
- IoC container disposed synchronously
- Not providing DisposeAsyncCore for inheritance

**Consequences:**
- Runtime exceptions when container disposed synchronously
- Deadlocks from improper async disposal
- ConfigureAwait setting not applied to DisposeAsync
- Inheritance issues (derived classes can't override disposal)
- Resource leaks

**Prevention:**
1. **Don't `await using` something obtained asynchronously:**
   ```csharp
   // WRONG: ConfigureAwait doesn't apply to DisposeAsync
   await using var resource = await GetResourceAsync().ConfigureAwait(false);

   // CORRECT: Separate acquisition and disposal
   var resource = await GetResourceAsync().ConfigureAwait(false);
   await using (resource.ConfigureAwait(false))
   {
       // Use resource
   }
   ```

2. **Implement both IAsyncDisposable AND IDisposable:**
   ```csharp
   public sealed class MyResource : IAsyncDisposable, IDisposable
   {
       private bool _disposed;

       public async ValueTask DisposeAsync()
       {
           if (_disposed) return;

           await DisposeAsyncCore().ConfigureAwait(false);

           Dispose(disposing: false);
           GC.SuppressFinalize(this);

           _disposed = true;
       }

       protected virtual async ValueTask DisposeAsyncCore()
       {
           // Async cleanup here
           await CloseConnectionAsync().ConfigureAwait(false);
       }

       public void Dispose()
       {
           if (_disposed) return;

           Dispose(disposing: true);
           GC.SuppressFinalize(this);

           _disposed = true;
       }

       protected virtual void Dispose(bool disposing)
       {
           if (disposing)
           {
               // Sync cleanup here
               CloseConnection();
           }
       }
   }
   ```

3. **For non-sealed classes, provide DisposeAsyncCore:**
   ```csharp
   public class BaseResource : IAsyncDisposable
   {
       protected virtual async ValueTask DisposeAsyncCore()
       {
           // Base cleanup
       }

       public async ValueTask DisposeAsync()
       {
           await DisposeAsyncCore().ConfigureAwait(false);
           GC.SuppressFinalize(this);
       }
   }

   public class DerivedResource : BaseResource
   {
       protected override async ValueTask DisposeAsyncCore()
       {
           // Derived cleanup
           await base.DisposeAsyncCore().ConfigureAwait(false);
       }
   }
   ```

4. **Be aware of IoC container disposal timing** - If container disposed synchronously, DisposeAsync may not be called

**Detection:**
- Runtime exceptions on application shutdown
- Resource leaks (connections not closed)
- ConfigureAwait settings not respected
- Inheritance issues with disposal

**Phase mapping:**
- Phase X (EF Core integration): Review DbContext disposal patterns
- Phase X: Any async disposable resources need both interfaces

**Sources:**
- [IAsyncDisposable Pitfalls - ZpqrtBnk](https://www.zpqrtbnk.net/posts/iasyncdisposable-pitfalls/)
- [Implement a DisposeAsync method - .NET - Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/standard/garbage-collection/implementing-disposeasync)

---

## Phase-Specific Warnings

| Phase Topic | Likely Pitfall | Mitigation |
|-------------|----------------|------------|
| Core Library Foundation | Dependency creep, over-exposing public API | Establish zero-dependency policy, default to internal |
| NuGet Package Setup | CPM misconfiguration, metadata missing | Configure CPM at solution root, complete metadata before first publish |
| Versioning Strategy | AssemblyVersion/PackageVersion out of sync | Document versioning strategy, configure all four versions |
| EF Core Audit Logging | Transaction scope confusion, orphaned audit records | Buffer audit events in interceptor, write in same transaction |
| Attribute System | Missing AttributeUsage, non-sealed classes, AOT incompatibility | Follow Microsoft guidelines, seal classes, mark AOT-incompatible APIs |
| Blazor Telemetry Integration | Server vs WASM assumptions, missing DI services | Test in both hosting models, use abstractions for hosting-specific features |
| Multi-Package Dependencies | Viral dependencies, diamond conflicts | Keep core dependency-free, use PrivateAssets for build-time deps |
| Public API Review | Too much public surface | Review public API before each release, use API analyzer |
| Breaking Changes | Assembly identity changes, SemVer violations | Only increment AssemblyVersion for MAJOR, follow SemVer strictly |
| Testing Strategy | InternalsVisibleTo misconfiguration | Configure strong-name public key if needed, test in both Debug/Release |

---

## Research Confidence Assessment

| Area | Confidence | Basis |
|------|------------|-------|
| Dependency Management | HIGH | Microsoft official docs + InfoQ enterprise article + recent experience (2025-2026) |
| Versioning & Breaking Changes | HIGH | Microsoft official library guidance + multiple authoritative sources |
| Native AOT / Trimming | HIGH | Official .NET Blog article on AOT-compatible libraries + recent Medium article (2025) |
| EF Core Audit Logging | MEDIUM-HIGH | Multiple recent articles (DEV Community, Elmah.io) + Microsoft transactions docs |
| Blazor Integration | MEDIUM | Official Q&A, GitHub issues, community articles (Gap Velocity, Medium 2026 trends) |
| Attribute Design | HIGH | Microsoft official design guidelines + multiple implementation guides |
| NuGet Package Management | HIGH | Microsoft CPM docs + recent Medium article (Dec 2025) + InfoQ article |
| DbContext Lifetime | HIGH | Microsoft official docs + multiple recent blog posts (Ardalis, Haacked, Code Maze) |
| IAsyncDisposable | MEDIUM-HIGH | Specific pitfall article (ZpqrtBnk) + Microsoft official docs |
| XML Documentation | MEDIUM-HIGH | Microsoft official docs + community guidelines |

---

## Gaps and Open Questions

**Areas with sufficient research:**
- All major categories covered with authoritative sources
- Recent sources (2025-2026) validate current best practices
- Microsoft official documentation provides canonical guidance

**Areas that may need phase-specific research:**
- **Blazor Application Insights integration specifics** - Community packages vs custom implementation (LOW confidence - rely on testing)
- **Source generators for AOT compatibility** - If implementing compile-time alternatives to reflection (MEDIUM confidence - design phase research)
- **Multi-package library monorepo tooling** - Build orchestration, versioning automation (MEDIUM confidence - setup phase research)
- **EF Core interceptor performance at scale** - Load testing audit logging write throughput (LOW confidence - needs empirical testing)

**Recommended approach:**
- Use this research for roadmap/phase planning
- Flag phases requiring deeper research (EF Core audit, Blazor telemetry)
- Conduct phase-specific research when implementation begins

---

## Sources

### Dependency Management
- [Dependencies and .NET libraries - Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/standard/library-guidance/dependencies)
- [Best Practices for Managing Shared Libraries in .NET Applications at Scale - InfoQ](https://www.infoq.com/articles/shared-libraries-dotnet-enterprise/)
- [Dependency Management in .NET Libraries - Medium](https://medium.com/@osama.abusitta/dependency-management-in-net-libraries-a-guide-for-library-authors-part-2-37a76a8559af)

### Breaking Changes & Versioning
- [Breaking changes and .NET libraries - Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/standard/library-guidance/breaking-changes)
- [Versioning and .NET libraries - Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/standard/library-guidance/versioning)
- [Strong naming and .NET libraries - Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/standard/library-guidance/strong-naming)

### Native AOT & Trimming
- [How to make libraries compatible with native AOT - .NET Blog](https://devblogs.microsoft.com/dotnet/creating-aot-compatible-libraries/)
- [Surviving Native AOT: The Reflection Migration Guide - Medium](https://medium.com/@dikhyantkrishnadalai/surviving-native-aot-the-reflection-migration-guide-every-net-architect-needs-fa3760fbb41b)
- [Introduction to AOT warnings - .NET - Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/fixing-warnings)

### EF Core Audit Logging
- [Comprehensive Guide to Implementing Audit Logging in .NET with EF Core Interceptors - DEV Community](https://dev.to/hootanht/comprehensive-guide-to-implementing-audit-logging-in-net-with-ef-core-interceptors-1e83)
- [EF Core Transaction Guide - Devart](https://www.devart.com/dotconnect/ef-core-transactions.html)
- [3 Essential Techniques for Managing Transactions in EF Core - Elmah.io](https://blog.elmah.io/3-essential-techniques-for-managing-transactions-in-ef-core/)
- [Implementing Audit Logging in .NET - DevelopersVoice](https://developersvoice.com/blog/database/efcore_audit_implementation_guide/)

### Blazor Integration
- [Blazor Server vs. WebAssembly: Which Works Best For You? - Gap Velocity](https://www.gapvelocity.ai/blog/blazor-server-vs-webassembly)
- [Emerging Trends in Blazor Development for 2026 - Medium](https://medium.com/@reenbit/emerging-trends-in-blazor-development-for-2026-70d6a52e3d2a)
- [BlazorApplicationInsights - GitHub](https://github.com/IvanJosipovic/BlazorApplicationInsights)
- [Application Insights and Blazor Server - GitHub Issue](https://github.com/dotnet/AspNetCore.Docs/issues/30546)

### Attribute Design
- [Attributes (.NET Framework design guidelines) - Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/attributes)
- [Writing Custom Attributes - .NET - Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/standard/attributes/writing-custom-attributes)

### NuGet Package Management
- [Central Package Management (CPM) - NuGet - Microsoft Learn](https://learn.microsoft.com/en-us/nuget/consume-packages/central-package-management)
- [Implementing NuGet Central Package Management in Visual Studio 2026 - Medium](https://sachidevop.medium.com/implementing-nuget-central-package-management-cpm-in-a-net-solution-using-visual-studio-2026-c93f207edcb6)
- [NuGet PackageReference in project files - Microsoft Learn](https://learn.microsoft.com/en-us/nuget/consume-packages/package-references-in-project-files)
- [Package authoring best practices - NuGet - Microsoft Learn](https://learn.microsoft.com/en-us/nuget/create-packages/package-authoring-best-practices)

### DbContext Lifetime
- [DbContext Lifetime, Configuration, and Initialization - EF Core - Microsoft Learn](https://learn.microsoft.com/en-us/ef/core/dbcontext-configuration/)
- [Avoid Wrapping DbContext in Using - Ardalis](https://ardalis.com/avoid-wrapping-dbcontext-in-using/)
- [When Your DbContext Has The Wrong Scope - Haacked](https://haacked.com/archive/2023/01/09/scoping-db-context-copy/)

### IAsyncDisposable
- [IAsyncDisposable Pitfalls - ZpqrtBnk](https://www.zpqrtbnk.net/posts/iasyncdisposable-pitfalls/)
- [Implement a DisposeAsync method - .NET - Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/standard/garbage-collection/implementing-disposeasync)

### XML Documentation
- [Recommended XML documentation tags - C# reference - Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/xmldoc/recommended-tags)
- [Guidelines to Better XML Doc Comments - Helixoft](https://www.helixoft.com/blog/guidelines-to-better-xml-doc-comments-and-documentation.html)

### Friend Assemblies
- [Friend assemblies - .NET - Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/standard/assembly/friend)
- [Testing internal members with InternalsVisibleTo - Code4IT](https://www.code4it.dev/blog/testing-internals-with-internalsvisibleto/)

### General Library Development
- [Get started creating high-quality .NET libraries - Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/standard/library-guidance/get-started)
- [Top 15 Mistakes .NET Developers Make - Anton Dev Tips](https://antondevtips.com/blog/top-15-mistakes-dotnet-developers-make-how-to-avoid-common-pitfalls)
- [Creating and Maintaining .NET Libraries - Zartis](https://www.zartis.com/creating-and-maintaining-dotnet-libraries/)
