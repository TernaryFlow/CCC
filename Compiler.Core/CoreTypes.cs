namespace CSharpWorkers.Core;

/// <summary>
/// Represents a node in the intermediate representation.
/// </summary>
public abstract class IrNode
{
    public string Id { get; } = Guid.NewGuid().ToString("N")[..8];
    public SourceLocation? Location { get; set; }
}

/// <summary>
/// Source code location information.
/// </summary>
public record SourceLocation(string FilePath, int Line, int Column);

/// <summary>
/// Base type for all type representations.
/// </summary>
public abstract class TypeSymbol
{
    public string Name { get; init; } = "";
    public string Namespace { get; init; } = "";
    public bool IsReferenceType { get; init; }
    public bool IsNullable { get; init; }
    public bool IsGeneric { get; init; }
    public List<TypeSymbol> GenericArguments { get; init; } = new();
    
    public string FullName => string.IsNullOrEmpty(Namespace) ? Name : $"{Namespace}.{Name}";
    
    public override string ToString() => FullName;
}

/// <summary>
/// Represents a method signature.
/// </summary>
public record MethodSymbol(
    string Name,
    TypeSymbol ReturnType,
    List<ParameterSymbol> Parameters,
    bool IsAsync,
    bool IsStatic,
    bool IsVirtual,
    bool IsAbstract,
    bool IsOverride,
    TypeSymbol? DeclaringType
);

/// <summary>
/// Represents a method or function parameter.
/// </summary>
public record ParameterSymbol(string Name, TypeSymbol Type, bool IsOptional, object? DefaultValue = null);

/// <summary>
/// Compilation options for the compiler.
/// </summary>
public record CompilationOptions(
    string OutputPath = "./dist",
    bool Minify = false,
    bool SourceMaps = true,
    bool TreeShaking = true,
    bool Optimize = true,
    TargetRuntime Runtime = TargetRuntime.CloudflareWorkers
);

/// <summary>
/// Target runtime for compilation.
/// </summary>
public enum TargetRuntime
{
    CloudflareWorkers,
    NodeJS,
    Browser
}

/// <summary>
/// Diagnostic result from compilation.
/// </summary>
public record Diagnostic(
    DiagnosticSeverity Severity,
    string Code,
    string Message,
    SourceLocation? Location = null
);

/// <summary>
/// Severity level for diagnostics.
/// </summary>
public enum DiagnosticSeverity
{
    Info,
    Warning,
    Error
}

/// <summary>
/// Result of a compilation operation.
/// </summary>
public record CompilationResult(
    bool Success,
    string JavaScript,
    string? SourceMap = null,
    IReadOnlyList<Diagnostic> Diagnostics = default!
);
