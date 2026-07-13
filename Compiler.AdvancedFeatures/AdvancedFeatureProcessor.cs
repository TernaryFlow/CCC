using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Compiler.AdvancedFeatures
{
    /// <summary>
    /// Handles advanced C# features that require special treatment
    /// including Reflection, Dynamic, Expression Trees, Unsafe code, etc.
    /// </summary>
    public class AdvancedFeatureProcessor
    {
        private readonly Compilation _compilation;
        private readonly SemanticModel _semanticModel;
        private readonly List<Diagnostic> _diagnostics;
        private readonly AdvancedFeatureOptions _options;

        public AdvancedFeatureProcessor(
            Compilation compilation,
            SemanticModel semanticModel,
            AdvancedFeatureOptions options)
        {
            _compilation = compilation;
            _semanticModel = semanticModel;
            _options = options;
            _diagnostics = new List<Diagnostic>();
        }

        public IReadOnlyList<Diagnostic> Diagnostics => _diagnostics;

        /// <summary>
        /// Analyzes and processes advanced features in the syntax tree
        /// </summary>
        public AdvancedFeatureAnalysisResult Analyze(SyntaxNode root)
        {
            var visitor = new AdvancedFeatureVisitor(this);
            visitor.Visit(root);

            return new AdvancedFeatureAnalysisResult
            {
                HasReflection = visitor.HasReflection,
                HasDynamic = visitor.HasDynamic,
                HasExpressionTrees = visitor.HasExpressionTrees,
                HasUnsafeCode = visitor.HasUnsafeCode,
                HasPInvoke = visitor.HasPInvoke,
                HasThreading = visitor.HasThreading,
                HasAppDomain = visitor.HasAppDomain,
                HasMarshal = visitor.HasMarshal,
                HasCOM = visitor.HasCOM,
                ReflectionNodes = visitor.ReflectionNodes,
                DynamicNodes = visitor.DynamicNodes,
                ExpressionTreeNodes = visitor.ExpressionTreeNodes,
                UnsafeNodes = visitor.UnsafeNodes,
                PInvokeNodes = visitor.PInvokeNodes,
                ThreadingNodes = visitor.ThreadingNodes,
                AppDomainNodes = visitor.AppDomainNodes,
                MarshalNodes = visitor.MarshalNodes,
                COMNodes = visitor.COMNodes,
                Diagnostics = _diagnostics
            };
        }

        internal void ReportDiagnostic(Diagnostic diagnostic)
        {
            _diagnostics.Add(diagnostic);
        }

        internal Diagnostic CreateDiagnostic(
            Location location,
            string featureName,
            string message,
            DiagnosticSeverity severity = DiagnosticSeverity.Warning)
        {
            return Diagnostic.Create(
                new DiagnosticDescriptor(
                    id: $"CSW_ADV_{featureName.GetHashCode() % 1000:D3}",
                    title: $"Advanced Feature: {featureName}",
                    messageFormat: message,
                    category: "AdvancedFeatures",
                    defaultSeverity: severity,
                    isEnabledByDefault: true),
                location);
        }
    }

    public class AdvancedFeatureOptions
    {
        /// <summary>
        /// Enable limited reflection support (type names only)
        /// </summary>
        public bool EnableLimitedReflection { get; set; } = false;

        /// <summary>
        /// Enable dynamic keyword with runtime binding simulation
        /// </summary>
        public bool EnableDynamic { get; set; } = false;

        /// <summary>
        /// Enable expression tree compilation to JavaScript
        /// </summary>
        public bool EnableExpressionTrees { get; set; } = false;

        /// <summary>
        /// Allow unsafe code blocks (converted to safe JS equivalents where possible)
        /// </summary>
        public bool AllowUnsafeCode { get; set; } = false;

        /// <summary>
        /// Enable P/Invoke simulation through JavaScript interop
        /// </summary>
        public bool EnablePInvoke { get; set; } = false;

        /// <summary>
        /// Enable threading primitives beyond async/await
        /// </summary>
        public bool EnableThreading { get; set; } = false;

        /// <summary>
        /// Enable AppDomain simulation (limited)
        /// </summary>
        public bool EnableAppDomain { get; set; } = false;

        /// <summary>
        /// Enable Marshal operations simulation
        /// </summary>
        public bool EnableMarshal { get; set; } = false;

        /// <summary>
        /// Enable COM interop simulation
        /// </summary>
        public bool EnableCOM { get; set; } = false;

        /// <summary>
        /// Treat unsupported features as errors instead of warnings
        /// </summary>
        public bool TreatUnsupportedAsErrors { get; set; } = false;
    }

    public class AdvancedFeatureAnalysisResult
    {
        public bool HasReflection { get; init; }
        public bool HasDynamic { get; init; }
        public bool HasExpressionTrees { get; init; }
        public bool HasUnsafeCode { get; init; }
        public bool HasPInvoke { get; init; }
        public bool HasThreading { get; init; }
        public bool HasAppDomain { get; init; }
        public bool HasMarshal { get; init; }
        public bool HasCOM { get; init; }

        public List<SyntaxNode> ReflectionNodes { get; init; } = new();
        public List<SyntaxNode> DynamicNodes { get; init; } = new();
        public List<SyntaxNode> ExpressionTreeNodes { get; init; } = new();
        public List<SyntaxNode> UnsafeNodes { get; init; } = new();
        public List<SyntaxNode> PInvokeNodes { get; init; } = new();
        public List<SyntaxNode> ThreadingNodes { get; init; } = new();
        public List<SyntaxNode> AppDomainNodes { get; init; } = new();
        public List<SyntaxNode> MarshalNodes { get; init; } = new();
        public List<SyntaxNode> COMNodes { get; init; } = new();

        public IReadOnlyList<Diagnostic> Diagnostics { get; init; } = Array.Empty<Diagnostic>();

        public bool HasAnyAdvancedFeatures =>
            HasReflection || HasDynamic || HasExpressionTrees ||
            HasUnsafeCode || HasPInvoke || HasThreading ||
            HasAppDomain || HasMarshal || HasCOM;
    }

    internal class AdvancedFeatureVisitor : CSharpSyntaxWalker
    {
        private readonly AdvancedFeatureProcessor _processor;

        public bool HasReflection { get; private set; }
        public bool HasDynamic { get; private set; }
        public bool HasExpressionTrees { get; private set; }
        public bool HasUnsafeCode { get; private set; }
        public bool HasPInvoke { get; private set; }
        public bool HasThreading { get; private set; }
        public bool HasAppDomain { get; private set; }
        public bool HasMarshal { get; private set; }
        public bool HasCOM { get; private set; }

        public List<SyntaxNode> ReflectionNodes { get; } = new();
        public List<SyntaxNode> DynamicNodes { get; } = new();
        public List<SyntaxNode> ExpressionTreeNodes { get; } = new();
        public List<SyntaxNode> UnsafeNodes { get; } = new();
        public List<SyntaxNode> PInvokeNodes { get; } = new();
        public List<SyntaxNode> ThreadingNodes { get; } = new();
        public List<SyntaxNode> AppDomainNodes { get; } = new();
        public List<SyntaxNode> MarshalNodes { get; } = new();
        public List<SyntaxNode> COMNodes { get; } = new();

        public AdvancedFeatureVisitor(AdvancedFeatureProcessor processor)
        {
            _processor = processor;
        }

        public override void VisitCompilationUnit(CompilationUnitSyntax node)
        {
            // Check for unsafe context
            if (node.DescendantNodes().OfType<UnsafeStatementSyntax>().Any() ||
                node.DescendantNodes().OfType<UnsafeKeywordSyntax>().Any())
            {
                HasUnsafeCode = true;
                foreach (var unsafeNode in node.DescendantNodes().OfType<UnsafeStatementSyntax>())
                {
                    UnsafeNodes.Add(unsafeNode);
                    HandleUnsafeCode(unsafeNode);
                }
            }

            base.VisitCompilationUnit(node);
        }

        public override void VisitTypeOfExpression(TypeOfExpressionSyntax node)
        {
            HasReflection = true;
            ReflectionNodes.Add(node);
            HandleReflection(node, "typeof expression");
            base.VisitTypeOfExpression(node);
        }

        public override void VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
        {
            // Check for reflection patterns
            var symbol = _processor._semanticModel.GetSymbolInfo(node).Symbol;
            if (symbol != null)
            {
                var typeName = symbol.ContainingType?.ToDisplayString();
                if (typeName == "System.Type" || typeName?.StartsWith("System.Reflection") == true)
                {
                    HasReflection = true;
                    ReflectionNodes.Add(node);
                    HandleReflection(node, $"Reflection access: {node}");
                }
            }

            // Check for dynamic invocation
            var typeInfo = _processor._semanticModel.GetTypeInfo(node.Expression);
            if (typeInfo.Type?.SpecialType == SpecialType.System_DynamicObject ||
                typeInfo.ConvertedType?.TypeKind == TypeKind.Dynamic)
            {
                HasDynamic = true;
                DynamicNodes.Add(node);
                HandleDynamic(node);
            }

            base.VisitMemberAccessExpression(node);
        }

        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            var symbol = _processor._semanticModel.GetSymbolInfo(node).Symbol;
            if (symbol != null)
            {
                var typeName = symbol.ContainingType?.ToDisplayString();

                // Check for Expression.Tree methods
                if (typeName?.StartsWith("System.Linq.Expressions.Expression") == true)
                {
                    HasExpressionTrees = true;
                    ExpressionTreeNodes.Add(node);
                    HandleExpressionTrees(node);
                }

                // Check for Marshal methods
                if (typeName?.StartsWith("System.Runtime.InteropServices.Marshal") == true)
                {
                    HasMarshal = true;
                    MarshalNodes.Add(node);
                    HandleMarshal(node);
                }

                // Check for AppDomain methods
                if (typeName?.StartsWith("System.AppDomain") == true)
                {
                    HasAppDomain = true;
                    AppDomainNodes.Add(node);
                    HandleAppDomain(node);
                }

                // Check for Thread-related types
                if (typeName?.StartsWith("System.Threading.Thread") == true ||
                    typeName?.StartsWith("System.Threading.Tasks.Parallel") == true ||
                    typeName?.Contains("Mutex") == true ||
                    typeName?.Contains("Semaphore") == true ||
                    typeName?.Contains("Monitor") == true)
                {
                    HasThreading = true;
                    ThreadingNodes.Add(node);
                    HandleThreading(node);
                }
            }

            base.VisitInvocationExpression(node);
        }

        public override void VisitVariableDeclaration(VariableDeclarationSyntax node)
        {
            if (node.Type is PredefinedTypeSyntax predefined &&
                predefined.Keyword.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.DynamicKeyword))
            {
                HasDynamic = true;
                DynamicNodes.Add(node);
                HandleDynamic(node);
            }

            var typeInfo = _processor._semanticModel.GetTypeInfo(node.Type);
            if (typeInfo.Type?.TypeKind == TypeKind.Dynamic)
            {
                HasDynamic = true;
                DynamicNodes.Add(node);
                HandleDynamic(node);
            }

            base.VisitVariableDeclaration(node);
        }

        public override void VisitLambdaExpression(LambdaExpressionSyntax node)
        {
            // Check if lambda is being converted to Expression<T>
            var typeInfo = _processor._semanticModel.GetTypeInfo(node);
            if (typeInfo.ConvertedType?.ToDisplayString()?.StartsWith("System.Linq.Expressions.Expression") == true)
            {
                HasExpressionTrees = true;
                ExpressionTreeNodes.Add(node);
                HandleExpressionTrees(node);
            }

            base.VisitLambdaExpression(node);
        }

        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            // Check for DllImport attribute (P/Invoke)
            foreach (var attribute in node.AttributeLists.SelectMany(al => al.Attributes))
            {
                var attributeName = attribute.Name.ToString();
                if (attributeName.Contains("DllImport") || attributeName.Contains("UnmanagedFunctionPointer"))
                {
                    HasPInvoke = true;
                    PInvokeNodes.Add(node);
                    HandlePInvoke(node, attribute);
                }

                // Check for COM-related attributes
                if (attributeName.Contains("ComImport") ||
                    attributeName.Contains("Guid") ||
                    attributeName.Contains("InterfaceType") ||
                    attributeName.Contains("ClassInterface"))
                {
                    HasCOM = true;
                    COMNodes.Add(node);
                    HandleCOM(node, attribute);
                }
            }

            base.VisitMethodDeclaration(node);
        }

        public override void VisitPointerType(PointerTypeSyntax node)
        {
            HasUnsafeCode = true;
            UnsafeNodes.Add(node);
            HandleUnsafeCode(node);
            base.VisitPointerType(node);
        }

        public override void VisitFixedStatement(FixedStatementSyntax node)
        {
            HasUnsafeCode = true;
            UnsafeNodes.Add(node);
            HandleUnsafeCode(node);
            base.VisitFixedStatement(node);
        }

        public override void VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
        {
            var symbol = _processor._semanticModel.GetSymbolInfo(node).Symbol;
            var typeName = symbol?.ContainingType?.ToDisplayString();

            // Check for Thread creation
            if (typeName?.Contains("Thread") == true ||
                typeName?.Contains("Mutex") == true ||
                typeName?.Contains("Semaphore") == true)
            {
                HasThreading = true;
                ThreadingNodes.Add(node);
                HandleThreading(node);
            }

            base.VisitObjectCreationExpression(node);
        }

        private void HandleReflection(SyntaxNode node, string description)
        {
            if (!_processor._options.EnableLimitedReflection)
            {
                var severity = _processor._options.TreatUnsupportedAsErrors
                    ? DiagnosticSeverity.Error
                    : DiagnosticSeverity.Warning;

                _processor.ReportDiagnostic(_processor.CreateDiagnostic(
                    node.GetLocation(),
                    "REFLECTION",
                    $"{description} requires reflection support. " +
                    $"Limited reflection (type names only) can be enabled via compiler options. " +
                    $"Full reflection is not available in Cloudflare Workers environment.",
                    severity));
            }
            else
            {
                _processor.ReportDiagnostic(_processor.CreateDiagnostic(
                    node.GetLocation(),
                    "REFLECTION",
                    $"{description} will use limited reflection (type names only). " +
                    $"Runtime type inspection is not available.",
                    DiagnosticSeverity.Info));
            }
        }

        private void HandleDynamic(SyntaxNode node)
        {
            if (!_processor._options.EnableDynamic)
            {
                var severity = _processor._options.TreatUnsupportedAsErrors
                    ? DiagnosticSeverity.Error
                    : DiagnosticSeverity.Warning;

                _processor.ReportDiagnostic(_processor.CreateDiagnostic(
                    node.GetLocation(),
                    "DYNAMIC",
                    "Dynamic keyword requires runtime binder which is not fully supported. " +
                    $"Consider using explicit typing or enable dynamic simulation (limited support).",
                    severity));
            }
            else
            {
                _processor.ReportDiagnostic(_processor.CreateDiagnostic(
                    node.GetLocation(),
                    "DYNAMIC",
                    "Dynamic invocation will be simulated at runtime. " +
                    $"Performance may be impacted and some operations may not be supported.",
                    DiagnosticSeverity.Warning));
            }
        }

        private void HandleExpressionTrees(SyntaxNode node)
        {
            if (!_processor._options.EnableExpressionTrees)
            {
                var severity = _processor._options.TreatUnsupportedAsErrors
                    ? DiagnosticSeverity.Error
                    : DiagnosticSeverity.Warning;

                _processor.ReportDiagnostic(_processor.CreateDiagnostic(
                    node.GetLocation(),
                    "EXPRESSION_TREES",
                    "Expression trees require compilation to intermediate representation. " +
                    $"Enable expression tree support to compile expressions to JavaScript functions.",
                    severity));
            }
            else
            {
                _processor.ReportDiagnostic(_processor.CreateDiagnostic(
                    node.GetLocation(),
                    "EXPRESSION_TREES",
                    "Expression tree will be compiled to JavaScript. " +
                    $"Only a subset of expression types are supported.",
                    DiagnosticSeverity.Info));
            }
        }

        private void HandleUnsafeCode(SyntaxNode node)
        {
            if (!_processor._options.AllowUnsafeCode)
            {
                var severity = _processor._options.TreatUnsupportedAsErrors
                    ? DiagnosticSeverity.Error
                    : DiagnosticSeverity.Warning;

                _processor.ReportDiagnostic(_processor.CreateDiagnostic(
                    node.GetLocation(),
                    "UNSAFE",
                    "Unsafe code (pointers, stackalloc, fixed) is not supported in JavaScript. " +
                    $"Refactor to use safe alternatives or enable unsafe code simulation (limited).",
                    severity));
            }
            else
            {
                _processor.ReportDiagnostic(_processor.CreateDiagnostic(
                    node.GetLocation(),
                    "UNSAFE",
                    "Unsafe code block will be analyzed. Pointer operations will be converted " +
                    $"to safe JavaScript array/buffer operations where possible. Some operations may throw at runtime.",
                    DiagnosticSeverity.Warning));
            }
        }

        private void HandlePInvoke(MethodDeclarationSyntax node, AttributeSyntax attribute)
        {
            if (!_processor._options.EnablePInvoke)
            {
                var severity = _processor._options.TreatUnsupportedAsErrors
                    ? DiagnosticSeverity.Error
                    : DiagnosticSeverity.Warning;

                _processor.ReportDiagnostic(_processor.CreateDiagnostic(
                    node.GetLocation(),
                    "PINVOKE",
                    "P/Invoke (Dllimport) cannot directly call native code in Cloudflare Workers. " +
                    $"Use JavaScript interop or WebAssembly modules instead.",
                    severity));
            }
            else
            {
                _processor.ReportDiagnostic(_processor.CreateDiagnostic(
                    node.GetLocation(),
                    "PINVOKE",
                    "P/Invoke declaration will be converted to JavaScript FFI call. " +
                    $"Ensure the target function is available in the Workers environment.",
                    DiagnosticSeverity.Warning));
            }
        }

        private void HandleThreading(SyntaxNode node)
        {
            if (!_processor._options.EnableThreading)
            {
                var severity = _processor._options.TreatUnsupportedAsErrors
                    ? DiagnosticSeverity.Error
                    : DiagnosticSeverity.Warning;

                _processor.ReportDiagnostic(_processor.CreateDiagnostic(
                    node.GetLocation(),
                    "THREADING",
                    "True multi-threading is not available in Cloudflare Workers (single-threaded event loop). " +
                    $"Use async/await for concurrency. Thread primitives will be simulated with async coordination.",
                    severity));
            }
            else
            {
                _processor.ReportDiagnostic(_processor.CreateDiagnostic(
                    node.GetLocation(),
                    "THREADING",
                    "Threading primitives will be simulated using async/await and JavaScript promises. " +
                    $"True parallel execution is not available; operations will be concurrent, not parallel.",
                    DiagnosticSeverity.Warning));
            }
        }

        private void HandleAppDomain(SyntaxNode node)
        {
            if (!_processor._options.EnableAppDomain)
            {
                var severity = _processor._options.TreatUnsupportedAsErrors
                    ? DiagnosticSeverity.Error
                    : DiagnosticSeverity.Warning;

                _processor.ReportDiagnostic(_processor.CreateDiagnostic(
                    node.GetLocation(),
                    "APPDOMAIN",
                    "AppDomain is a .NET CLR concept not available in JavaScript. " +
                    $"Use module isolation or Worker isolation for similar functionality.",
                    severity));
            }
            else
            {
                _processor.ReportDiagnostic(_processor.CreateDiagnostic(
                    node.GetLocation(),
                    "APPDOMAIN",
                    "AppDomain usage will be mapped to JavaScript module scope or Worker isolation. " +
                    $"Limited functionality available.",
                    DiagnosticSeverity.Warning));
            }
        }

        private void HandleMarshal(SyntaxNode node)
        {
            if (!_processor._options.EnableMarshal)
            {
                var severity = _processor._options.TreatUnsupportedAsErrors
                    ? DiagnosticSeverity.Error
                    : DiagnosticSeverity.Warning;

                _processor.ReportDiagnostic(_processor.CreateDiagnostic(
                    node.GetLocation(),
                    "MARSHAL",
                    "Marshal operations for unmanaged memory are not supported. " +
                    $"Use JavaScript ArrayBuffer and DataView for binary data manipulation.",
                    severity));
            }
            else
            {
                _processor.ReportDiagnostic(_processor.CreateDiagnostic(
                    node.GetLocation(),
                    "MARSHAL",
                    "Marshal operations will be converted to JavaScript ArrayBuffer/DataView operations. " +
                    $"Only basic marshaling is supported.",
                    DiagnosticSeverity.Warning));
            }
        }

        private void HandleCOM(SyntaxNode node, AttributeSyntax attribute)
        {
            var severity = _processor._options.TreatUnsupportedAsErrors
                ? DiagnosticSeverity.Error
                : DiagnosticSeverity.Warning;

            _processor.ReportDiagnostic(_processor.CreateDiagnostic(
                node.GetLocation(),
                "COM",
                "COM interop is not supported in Cloudflare Workers environment. " +
                $"COM is Windows-specific and requires native interop not available in Workers.",
                severity));
        }
    }
}
