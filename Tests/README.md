# C# Workers Tests

This directory contains tests for the C# to Cloudflare Workers compiler.

## Test Structure

```
Tests/
├── Unit/                    # Unit tests for individual components
│   ├── ParserTests.cs       # Roslyn parser tests
│   ├── SemanticTests.cs     # Semantic analyzer tests
│   ├── IrGenerationTests.cs # IR generation tests
│   └── CodeGenTests.cs      # JavaScript code generation tests
├── Integration/             # End-to-end integration tests
│   ├── CompilationTests.cs  # Full pipeline tests
│   └── WorkerTests.cs       # Worker functionality tests
├── Snapshots/               # Snapshot test expectations
│   ├── Output/              # Expected generated JavaScript
│   └── Runtime/             # Expected runtime behavior
└── Performance/             # Performance benchmarks
    ├── CompileTime.cs       # Compilation speed tests
    └── RuntimePerf.cs       # Runtime performance tests
```

## Running Tests

### Prerequisites

```bash
# Install .NET SDK 8.0+
dotnet --version

# Install Node.js for runtime tests
node --version

# Install wrangler for deployment tests
npm install -g wrangler
```

### Run All Tests

```bash
dotnet test
```

### Run Specific Test Categories

```bash
# Unit tests only
dotnet test --filter "Category=Unit"

# Integration tests only  
dotnet test --filter "Category=Integration"

# Snapshot tests only
dotnet test --filter "Category=Snapshot"

# Performance tests (longer running)
dotnet test --filter "Category=Performance"
```

### Update Snapshots

```bash
# Regenerate snapshot expectations
dotnet test -- UpdateSnapshots=true
```

## Writing Tests

### Unit Test Example

```csharp
using Xunit;
using CSharpWorkers.CodeGen;

public class CodeGenTests
{
    [Fact]
    public void GenerateClass_CreatesValidJavaScript()
    {
        // Arrange
        var generator = new JavaScriptCodeGenerator(new CompilationOptions());
        var classDecl = new ClassDeclaration 
        { 
            Name = "Test",
            Kind = TypeKind.Class
        };
        
        // Act
        var js = generator.Generate(new[] { classDecl });
        
        // Assert
        Assert.Contains("export class Test", js);
    }
}
```

### Integration Test Example

```csharp
using Xunit;

public class CompilationTests
{
    [Fact]
    public async Task FullPipeline_CompilesWorkerSuccessfully()
    {
        // Arrange
        var source = @"
            public class MyWorker : Worker
            {
                public override Task<Response> Fetch(Request r, Env e, ExecutionContext c)
                {
                    return Task.FromResult(Response.Json(new { ok = true }));
                }
            }";
        
        // Act
        var result = await Compiler.CompileAsync(source);
        
        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.JavaScript);
        Assert.Empty(result.Diagnostics.Where(d => d.Severity == Error));
    }
}
```

### Snapshot Test Example

```csharp
using VerifyTests;

[UsesVerify]
public class SnapshotTests
{
    [Fact]
    public Task GeneratesExpectedOutput()
    {
        // Arrange
        var source = File.ReadAllText("Input.cs");
        
        // Act
        var result = Compiler.Compile(source);
        
        // Assert - compare against snapshot
        return Verifier.Verify(result.JavaScript);
    }
}
```

## Performance Benchmarks

Run performance benchmarks:

```bash
dotnet run --project Tests/Performance/Benchmarks.csproj
```

Key metrics tracked:
- Compilation time per file
- Generated bundle size
- Runtime execution time
- Memory usage

## CI/CD Integration

Tests are automatically run on:
- Every pull request
- Every merge to main
- Nightly performance runs

See `.github/workflows/ci.yml` for configuration.
