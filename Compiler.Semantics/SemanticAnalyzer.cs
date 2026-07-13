using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using CSharpWorkers.Core;
using CSharpWorkers.Parser;

namespace CSharpWorkers.Semantics;

/// <summary>
/// Semantic analyzer that resolves types, methods, and symbols from Roslyn semantic model.
/// </summary>
public class SemanticAnalyzer
{
    private readonly SemanticModel _semanticModel;

    public SemanticAnalyzer(SemanticModel semanticModel)
    {
        _semanticModel = semanticModel;
    }

    /// <summary>
    /// Analyzes a type declaration and returns its symbol.
    /// </summary>
    public TypeSymbol AnalyzeType(ITypeSymbol typeSymbol)
    {
        return new TypeSymbol
        {
            Name = typeSymbol.Name,
            Namespace = typeSymbol.ContainingNamespace?.ToDisplayString() ?? "",
            IsReferenceType = typeSymbol.IsReferenceType,
            IsNullable = typeSymbol.NullableAnnotation == NullableAnnotation.Annotated,
            IsGeneric = typeSymbol is INamedTypeSymbol { IsGenericType: true },
            GenericArguments = typeSymbol is INamedTypeSymbol namedType 
                ? namedType.TypeArguments.Select(AnalyzeType).ToList() 
                : new List<TypeSymbol>()
        };
    }

    /// <summary>
    /// Analyzes a method declaration and returns its symbol.
    /// </summary>
    public MethodSymbol AnalyzeMethod(IMethodSymbol methodSymbol)
    {
        return new MethodSymbol(
            Name: methodSymbol.Name,
            ReturnType: AnalyzeType(methodSymbol.ReturnType),
            Parameters: methodSymbol.Parameters.Select(p => new ParameterSymbol(
                p.Name,
                AnalyzeType(p.Type),
                p.IsOptional,
                p.HasExplicitDefaultValue ? p.ExplicitDefaultValue : null
            )).ToList(),
            IsAsync: methodSymbol.IsAsync,
            IsStatic: methodSymbol.IsStatic,
            IsVirtual: methodSymbol.IsVirtual,
            IsAbstract: methodSymbol.IsAbstract,
            IsOverride: methodSymbol.IsOverride,
            DeclaringType: methodSymbol.ContainingType != null ? AnalyzeType(methodSymbol.ContainingType) : null
        );
    }

    /// <summary>
    /// Gets the type symbol for a syntax node.
    /// </summary>
    public ITypeSymbol? GetTypeInfo(SyntaxNode node)
    {
        var typeInfo = _semanticModel.GetTypeInfo(node);
        return typeInfo.Type;
    }

    /// <summary>
    /// Gets the symbol for a syntax node.
    /// </summary>
    public ISymbol? GetSymbol(SyntaxNode node)
    {
        return _semanticModel.GetDeclaredSymbol(node);
    }

    /// <summary>
    /// Resolves a type by its metadata name.
    /// </summary>
    public ITypeSymbol? ResolveType(string typeName)
    {
        return _semanticModel.Compilation.GetTypeByMetadataName(typeName);
    }
}

/// <summary>
/// Extension methods for working with Roslyn symbols.
/// </summary>
public static class SymbolExtensions
{
    /// <summary>
    /// Determines if a type is a known .NET type that has JavaScript equivalents.
    /// </summary>
    public static bool HasJavaScriptEquivalent(this ITypeSymbol type)
    {
        var fullName = type.ToDisplayString();
        return fullName switch
        {
            "System.String" or "System.Int32" or "System.Int64" or "System.Double" or 
            "System.Boolean" or "System.Object" or "System.DateTime" or "System.Guid" => true,
            "System.Collections.Generic.List`1" or "System.Collections.Generic.Dictionary`2" or
            "System.Collections.Generic.Queue`1" or "System.Collections.Generic.Stack`1" => true,
            "System.Threading.Tasks.Task" or "System.Threading.CancellationToken" => true,
            _ when type.TypeKind == TypeKind.Enum => true,
            _ when type.TypeKind == TypeKind.Class || type.TypeKind == TypeKind.Struct => true,
            _ => false
        };
    }

    /// <summary>
    /// Determines if a feature is supported by the compiler.
    /// </summary>
    public static bool IsSupportedFeature(this SyntaxNode node)
    {
        return node.Kind() switch
        {
            // Supported features
            SyntaxKind.ClassDeclaration or
            SyntaxKind.RecordDeclaration or
            SyntaxKind.StructDeclaration or
            SyntaxKind.InterfaceDeclaration or
            SyntaxKind.EnumDeclaration or
            SyntaxKind.MethodDeclaration or
            SyntaxKind.PropertyDeclaration or
            SyntaxKind.FieldDeclaration or
            SyntaxKind.EventDeclaration or
            SyntaxKind.DelegateDeclaration or
            SyntaxKind.SimpleLambdaExpression or
            SyntaxKind.ParenthesizedLambdaExpression or
            SyntaxKind.LocalFunctionStatement or
            SyntaxKind.SwitchExpression or
            SyntaxKind.SwitchStatement or
            SyntaxKind.IfStatement or
            SyntaxKind.WhileStatement or
            SyntaxKind.ForStatement or
            SyntaxKind.ForEachStatement or
            SyntaxKind.ReturnStatement or
            SyntaxKind.ThrowStatement or
            SyntaxKind.TryStatement or
            SyntaxKind.AwaitExpression or
            SyntaxKind.InvocationExpression or
            SyntaxKind.ObjectCreationExpression or
            SyntaxKind.ObjectInitializerExpression or
            SyntaxKind.CollectionInitializerExpression or
            SyntaxKind.ArrayInitializerExpression => true,
            
            // Unsupported features
            SyntaxKind.UnsafeStatement or
            SyntaxKind.FixedStatement or
            SyntaxKind.PointerType => false,
            
            _ => true // Default to supported for unknown kinds
        };
    }
}
