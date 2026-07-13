using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using CSharpWorkers.Core;

namespace CSharpWorkers.Parser;

/// <summary>
/// Roslyn-based C# parser that produces syntax trees and semantic models.
/// </summary>
public class CSharpParser
{
    private readonly CompilationOptions _options;

    public CSharpParser(CompilationOptions options)
    {
        _options = options;
    }

    /// <summary>
    /// Parses C# source code into a compilation unit.
    /// </summary>
    public ParsedCompilation Parse(string source, string filePath = "<source>")
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.CSharp12));
        
        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location))
            .Cast<MetadataReference>();

        var compilation = CSharpCompilation.Create(
            "worker",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );

        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var diagnostics = GetDiagnostics(compilation);

        return new ParsedCompilation(
            syntaxTree,
            semanticModel,
            diagnostics,
            filePath
        );
    }

    /// <summary>
    /// Parses multiple source files into a single compilation.
    /// </summary>
    public ParsedCompilation ParseFiles(IEnumerable<(string Path, string Content)> files)
    {
        var syntaxTrees = new List<SyntaxTree>();
        var fileMap = new Dictionary<string, string>();

        foreach (var (path, content) in files)
        {
            var tree = CSharpSyntaxTree.ParseText(content, new CSharpParseOptions(LanguageVersion.CSharp12));
            syntaxTrees.Add(tree);
            fileMap[tree.FilePath] = path;
        }

        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location))
            .Cast<MetadataReference>();

        var compilation = CSharpCompilation.Create(
            "worker",
            syntaxTrees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );

        var diagnostics = GetDiagnostics(compilation);

        return new ParsedCompilation(
            syntaxTrees.FirstOrDefault()?.GetRoot(),
            compilation,
            diagnostics,
            fileMap
        );
    }

    private static IReadOnlyList<Diagnostic> GetDiagnostics(CSharpCompilation compilation)
    {
        return compilation.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error || d.Severity == DiagnosticSeverity.Warning)
            .Select(d => new Diagnostic(
                (DiagnosticSeverity)(int)d.Severity,
                d.Id,
                d.GetMessage(),
                d.Location.IsInSource ? new SourceLocation(
                    d.Location.SourceTree?.FilePath ?? "<unknown>",
                    d.Location.GetLineSpan().StartLinePosition.Line + 1,
                    d.Location.GetLineSpan().StartLinePosition.Character + 1
                ) : null
            ))
            .ToList();
    }
}

/// <summary>
/// Result of parsing operation containing syntax tree and semantic model.
/// </summary>
public record ParsedCompilation(
    SyntaxNode? Root,
    SemanticModel SemanticModel,
    IReadOnlyList<Diagnostic> Diagnostics,
    string FilePath = "<source>"
);
