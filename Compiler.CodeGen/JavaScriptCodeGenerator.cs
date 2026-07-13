using System.Text;
using CSharpWorkers.Core;
using CSharpWorkers.IR;

namespace CSharpWorkers.CodeGen;

/// <summary>
/// JavaScript code generator that transforms IR into ES2025+ modules.
/// </summary>
public class JavaScriptCodeGenerator
{
    private readonly StringBuilder _output = new();
    private readonly CompilationOptions _options;
    private int _indentLevel;

    public JavaScriptCodeGenerator(CompilationOptions options)
    {
        _options = options;
    }

    /// <summary>
    /// Generates JavaScript module from a list of IR nodes.
    /// </summary>
    public string Generate(IEnumerable<IrNode> nodes)
    {
        _output.Clear();
        _indentLevel = 0;

        // Write module header
        WriteLine("'use strict';");
        WriteLine();

        // Import runtime helpers
        WriteLine("import { List, Dictionary, Queue, Stack, Task, CancellationToken } from './runtime.js';");
        WriteLine("import { Linq } from './linq.js';");
        WriteLine();

        // Generate each node
        foreach (var node in nodes)
        {
            if (node is ClassDeclaration classDecl)
            {
                GenerateClass(classDecl);
                WriteLine();
            }
        }

        // Export statement for worker entry point
        WriteLine("// Worker entry point");
        WriteLine("export default {");
        Indent();
        WriteLine("async fetch(request, env, ctx) {");
        Indent();
        WriteLine("const worker = new Worker();");
        WriteLine("return await worker.fetch(request, env, ctx);");
        Dedent();
        WriteLine("}");
        Dedent();
        WriteLine("};");

        return _output.ToString();
    }

    private void GenerateClass(ClassDeclaration classDecl)
    {
        var className = EscapeIdentifier(classDecl.Name);
        
        // Handle interfaces - just generate JSDoc since JS doesn't have interfaces
        if (classDecl.Kind == TypeKind.Interface)
        {
            WriteLine("/**");
            WriteLine($" * @interface {className}");
            foreach (var method in classDecl.Methods)
            {
                WriteLine($" * @method {method.Name}");
            }
            WriteLine(" */");
            return;
        }

        // Handle enums
        if (classDecl.Kind == TypeKind.Enum)
        {
            GenerateEnum(classDecl);
            return;
        }

        // Build class declaration
        var baseType = classDecl.BaseTypes.FirstOrDefault(t => t != "System.Object");
        var extendsClause = baseType != null ? $" extends {EscapeIdentifier(baseType)}" : "";

        WriteLine($"/**");
        WriteLine($" * {className} class");
        WriteLine($" */");
        WriteLine($"export class {className}{extendsClause} {{");
        Indent();

        // Generate fields as private class fields
        foreach (var field in classDecl.Fields)
        {
            var fieldName = EscapeIdentifier(field.Name);
            var initializer = field.Initializer != null ? GenerateExpression(field.Initializer) : null;
            
            if (field.IsStatic)
            {
                WriteLine($"static #{fieldName};");
            }
            else
            {
                WriteLine($"#{fieldName};");
            }
        }

        // Generate constructor
        GenerateConstructor(classDecl);

        // Generate properties
        foreach (var prop in classDecl.Properties)
        {
            GenerateProperty(prop);
        }

        // Generate methods
        foreach (var method in classDecl.Methods)
        {
            if (!method.Name.StartsWith(".")) // Skip constructors
            {
                GenerateMethod(method);
            }
        }

        Dedent();
        WriteLine("}");
    }

    private void GenerateConstructor(ClassDeclaration classDecl)
    {
        var constructor = classDecl.Methods.FirstOrDefault(m => m.Name == ".ctor");
        var parameters = constructor?.Parameters ?? new List<ParameterDeclaration>();

        Write($"constructor(");
        var paramStrings = parameters.Select(p => 
            p.Modifier == ParameterModifier.Out || p.Modifier == ParameterModifier.Ref 
                ? $"{EscapeIdentifier(p.Name)}Ref`" 
                : EscapeIdentifier(p.Name)
        ).ToList();
        Write(string.Join(", ", paramStrings));
        WriteLine(") {");
        Indent();

        // Initialize fields
        foreach (var field in classDecl.Fields.Where(f => !f.IsStatic && f.Initializer != null))
        {
            var fieldName = EscapeIdentifier(field.Name);
            var initExpr = GenerateExpression(field.Initializer);
            WriteLine($"this.#{fieldName} = {initExpr};");
        }

        // Call base constructor
        if (classDecl.BaseTypes.Any(t => t != "System.Object"))
        {
            WriteLine("super();");
        }

        // Execute constructor body
        if (constructor?.Body != null)
        {
            GenerateBlock(constructor.Body);
        }

        Dedent();
        WriteLine("}");
    }

    private void GenerateProperty(PropertyDeclaration prop)
    {
        var propName = EscapeIdentifier(prop.Name);
        var backingField = $"#_{propName}";
        
        if (prop.IsStatic)
        {
            WriteLine($"static {backingField};");
            WriteLine($"static get {propName}() {{ return this.{backingField}; }}");
            if (prop.HasSetter)
            {
                WriteLine($"static set {propName}(value) {{ this.{backingField} = value; }}");
            }
        }
        else
        {
            WriteLine($"{backingField};");
            WriteLine($"get {propName}() {{ return this.{backingField}; }}");
            if (prop.HasSetter)
            {
                WriteLine($"set {propName}(value) {{ this.{backingField} = value; }}");
            }
        }
    }

    private void GenerateMethod(MethodDeclaration method)
    {
        var methodName = EscapeIdentifier(method.Name);
        var isAsync = method.IsAsync;
        var isStatic = method.IsStatic;

        var asyncKeyword = isAsync ? "async " : "";
        var staticKeyword = isStatic ? "static " : "";

        Write($"{staticKeyword}{asyncKeyword}{methodName}(");
        var paramStrings = method.Parameters.Select(p => 
            p.Modifier == ParameterModifier.Out || p.Modifier == ParameterModifier.Ref 
                ? $"{EscapeIdentifier(p.Name)}Ref`" 
                : EscapeIdentifier(p.Name)
        ).ToList();
        Write(string.Join(", ", paramStrings));
        WriteLine(") {");
        Indent();

        if (method.Body != null)
        {
            GenerateBlock(method.Body);
        }
        else if (method.IsAbstract)
        {
            WriteLine("// Abstract method - must be overridden");
        }

        Dedent();
        WriteLine("}");
    }

    private void GenerateEnum(ClassDeclaration enumDecl)
    {
        var enumName = EscapeIdentifier(enumDecl.Name);
        WriteLine($"export const {enumName} = {{");
        Indent();

        var members = enumDecl.Fields.Select(f => f.Name).ToList();
        for (int i = 0; i < members.Count; i++)
        {
            var suffix = i < members.Count - 1 ? "," : "";
            WriteLine($"{members[i]}: {i}{suffix}");
        }

        Dedent();
        WriteLine("};");
        WriteLine();
        WriteLine($"Object.freeze({enumName});");
    }

    private void GenerateBlock(BlockStatement block)
    {
        foreach (var stmt in block.Statements)
        {
            GenerateStatement(stmt);
        }
    }

    private void GenerateStatement(Statement stmt)
    {
        switch (stmt)
        {
            case VariableDeclaration varDecl:
                var varType = GetJavaScriptType(varDecl.Type);
                var initValue = varDecl.Initializer != null ? GenerateExpression(varDecl.Initializer) : "undefined";
                WriteLine($"let {EscapeIdentifier(varDecl.Name)} = {initValue};");
                break;

            case ExpressionStatement exprStmt:
                WriteLine($"{GenerateExpression(exprStmt.Expression)};");
                break;

            case ReturnStatement returnStmt:
                var returnValue = returnStmt.Value != null ? GenerateExpression(returnStmt.Value) : "";
                WriteLine($"return {returnValue};");
                break;

            case IfStatement ifStmt:
                WriteLine($"if ({GenerateExpression(ifStmt.Condition)}) {{");
                Indent();
                GenerateStatement(ifStmt.ThenBranch);
                Dedent();
                if (ifStmt.ElseBranch != null)
                {
                    WriteLine("} else {");
                    Indent();
                    GenerateStatement(ifStmt.ElseBranch);
                    Dedent();
                }
                WriteLine("}");
                break;

            case WhileStatement whileStmt:
                WriteLine($"while ({GenerateExpression(whileStmt.Condition)}) {{");
                Indent();
                GenerateStatement(whileStmt.Body);
                Dedent();
                WriteLine("}");
                break;

            case ForStatement forStmt:
                Write("for (");
                if (forStmt.Initializer != null)
                {
                    var initType = GetJavaScriptType(forStmt.Initializer.Type);
                    var forInitValue = forStmt.Initializer.Initializer != null 
                        ? GenerateExpression(forStmt.Initializer.Initializer) 
                        : "undefined";
                    Write($"let {EscapeIdentifier(forStmt.Initializer.Name)} = {forInitValue}; ");
                }
                Write("; ");
                if (forStmt.Condition != null)
                {
                    Write(GenerateExpression(forStmt.Condition));
                }
                Write("; ");
                if (forStmt.Incrementor != null)
                {
                    Write(GenerateExpression(forStmt.Incrementor));
                }
                WriteLine(") {");
                Indent();
                GenerateStatement(forStmt.Body);
                Dedent();
                WriteLine("}");
                break;

            case ForEachStatement forEachStmt:
                var collectionExpr = GenerateExpression(forEachStmt.Collection);
                WriteLine($"for (const {EscapeIdentifier(forEachStmt.VariableName)} of {collectionExpr}) {{");
                Indent();
                GenerateStatement(forEachStmt.Body);
                Dedent();
                WriteLine("}");
                break;

            case TryStatement tryStmt:
                WriteLine("try {");
                Indent();
                GenerateBlock(tryStmt.TryBlock);
                Dedent();
                
                foreach (var catchClause in tryStmt.CatchClauses)
                {
                    var exceptionVar = catchClause.VariableName ?? "ex";
                    WriteLine($"}} catch ({EscapeIdentifier(exceptionVar)}) {{");
                    Indent();
                    GenerateBlock(catchClause.Body);
                    Dedent();
                }

                if (tryStmt.FinallyBlock != null)
                {
                    WriteLine("} finally {");
                    Indent();
                    GenerateBlock(tryStmt.FinallyBlock);
                    Dedent();
                }
                WriteLine("}");
                break;

            case ThrowStatement throwStmt:
                var exceptionExpr = throwStmt.Exception != null 
                    ? GenerateExpression(throwStmt.Exception) 
                    : "new Error('Unknown error')";
                WriteLine($"throw {exceptionExpr};");
                break;

            default:
                WriteLine($"// TODO: Generate statement {stmt.GetType().Name}");
                break;
        }
    }

    private string GenerateExpression(Expression expr)
    {
        return expr switch
        {
            LiteralExpression lit => GenerateLiteral(lit),
            IdentifierExpression id => EscapeIdentifier(id.Name),
            BinaryExpression bin => GenerateBinary(bin),
            UnaryExpression unary => GenerateUnary(unary),
            MemberAccessExpression member => $"{GenerateExpression(member.Target)}.{EscapeIdentifier(member.MemberName)}",
            InvocationExpression invoke => GenerateInvocation(invoke),
            ObjectCreationExpression obj => GenerateObjectCreation(obj),
            ArrayCreationExpression arr => GenerateArrayCreation(arr),
            AnonymousObjectCreationExpression anon => GenerateAnonymousObject(anon),
            LambdaExpression lambda => GenerateLambda(lambda),
            ConditionalExpression cond => $"{GenerateExpression(cond.Condition)} ? {GenerateExpression(cond.WhenTrue)} : {GenerateExpression(cond.WhenFalse)}",
            NullCoalescingExpression nullCoal => $"{GenerateExpression(nullCoal.Left)} ?? {GenerateExpression(nullCoal.Right)}",
            AwaitExpression awaitExpr => $"await {GenerateExpression(awaitExpr.InnerExpression)}",
            SwitchExpression switchExpr => GenerateSwitch(switchExpr),
            CastExpression cast => GenerateExpression(cast.Expression), // Type casts are erased in JS
            _ => $"/* TODO: {expr.GetType().Name} */ undefined"
        };
    }

    private string GenerateLiteral(LiteralExpression lit)
    {
        return lit.Kind switch
        {
            LiteralKind.String => $"\"{lit.Value?.ToString()?.Replace("\"", "\\\"") ?? ""}\"",
            LiteralKind.Boolean => lit.Value is bool b ? (b ? "true" : "false") : "false",
            LiteralKind.Integer => lit.Value?.ToString() ?? "0",
            LiteralKind.Float => lit.Value?.ToString() ?? "0.0",
            LiteralKind.Null => "null",
            LiteralKind.Character => $"'{lit.Value?.ToString() ?? ""}'",
            _ => "undefined"
        };
    }

    private string GenerateBinary(BinaryExpression bin)
    {
        var left = GenerateExpression(bin.Left);
        var right = GenerateExpression(bin.Right);
        
        var op = bin.Operator switch
        {
            BinaryOperator.Addition => "+",
            BinaryOperator.Subtraction => "-",
            BinaryOperator.Multiplication => "*",
            BinaryOperator.Division => "/",
            BinaryOperator.Modulo => "%",
            BinaryOperator.Equality => "===",
            BinaryOperator.Inequality => "!==",
            BinaryOperator.LessThan => "<",
            BinaryOperator.LessThanOrEqual => "<=",
            BinaryOperator.GreaterThan => ">",
            BinaryOperator.GreaterThanOrEqual => ">=",
            BinaryOperator.LogicalAnd => "&&",
            BinaryOperator.LogicalOr => "||",
            BinaryOperator.BitwiseAnd => "&",
            BinaryOperator.BitwiseOr => "|",
            BinaryOperator.BitwiseXor => "^",
            _ => "?"
        };

        return $"({left} {op} {right})";
    }

    private string GenerateUnary(UnaryExpression unary)
    {
        var operand = GenerateExpression(unary.Operand);
        
        return unary.Operator switch
        {
            UnaryOperator.Negation => $"(-{operand})",
            UnaryOperator.LogicalNot => $"(!{operand})",
            UnaryOperator.BitwiseNot => $"(~{operand})",
            UnaryOperator.PrefixIncrement => $"(++{operand})",
            UnaryOperator.PrefixDecrement => $"(--{operand})",
            UnaryOperator.PostfixIncrement => $"({operand}++)",
            UnaryOperator.PostfixDecrement => $"({operand}--)",
            _ => operand
        };
    }

    private string GenerateInvocation(InvocationExpression invoke)
    {
        var target = GenerateExpression(invoke.Target);
        var args = string.Join(", ", invoke.Arguments.Select(GenerateExpression));
        
        // Handle LINQ extension methods
        if (invoke.MethodName.StartsWith("LINQ."))
        {
            var linqMethod = invoke.MethodName.Substring(5);
            return $"{target}.{ToCamelCase(linqMethod)}({args})";
        }

        return $"{target}({args})";
    }

    private string GenerateObjectCreation(ObjectCreationExpression obj)
    {
        var typeName = EscapeIdentifier(obj.Type.Name);
        var args = string.Join(", ", obj.Arguments.Select(GenerateExpression));
        
        var result = $"new {typeName}({args})";
        
        if (obj.Initializers.Count > 0)
        {
            // Object initializer
            var initParts = obj.Initializers.Select(i => 
                $"{EscapeIdentifier(i.Name!)}: {GenerateExpression(i.Value)}"
            );
            result = $"Object.assign({result}, {{ {string.Join(", ", initParts)} }})";
        }

        return result;
    }

    private string GenerateArrayCreation(ArrayCreationExpression arr)
    {
        if (arr.Initializers != null && arr.Initializers.Count > 0)
        {
            var elements = string.Join(", ", arr.Initializers.Select(GenerateExpression));
            return $"[{elements}]";
        }
        
        var size = arr.Sizes?.FirstOrDefault();
        if (size != null)
        {
            return $"Array({GenerateExpression(size)})";
        }

        return "[]";
    }

    private string GenerateAnonymousObject(AnonymousObjectCreationExpression anon)
    {
        var members = anon.Members.Select(m => 
            $"{EscapeIdentifier(m.Name)}: {GenerateExpression(m.Value)}"
        );
        return $"{{ {string.Join(", ", members)} }}";
    }

    private string GenerateLambda(LambdaExpression lambda)
    {
        var paramsList = string.Join(", ", lambda.Parameters.Select(p => EscapeIdentifier(p.Name)));
        
        if (lambda.ExpressionBody != null)
        {
            return $"({paramsList}) => {GenerateExpression(lambda.ExpressionBody)}";
        }
        
        return $"async ({paramsList}) => {{ /* block body */ }}";
    }

    private string GenerateSwitch(SwitchExpression switchExpr)
    {
        var input = GenerateExpression(switchExpr.Input);
        var arms = switchExpr.Arms.Select(arm =>
        {
            var condition = arm.Pattern switch
            {
                ConstantPattern cp => $"{input} === {GenerateExpression(cp.Value)}",
                DiscardPattern => "true",
                _ => "true"
            };
            
            if (arm.Guard != null)
            {
                condition = $"{condition} && {GenerateExpression(arm.Guard)}";
            }
            
            return $"{condition} ? {GenerateExpression(arm.Result)}";
        });

        return arms.Aggregate((acc, next) => $"{acc} : {next}");
    }

    private static string GetJavaScriptType(TypeSymbol type)
    {
        return type.FullName switch
        {
            "System.String" => "string",
            "System.Int32" or "System.Int64" or "System.Double" or "System.Single" => "number",
            "System.Boolean" => "boolean",
            "System.Object" => "object",
            _ => "any"
        };
    }

    private static string EscapeIdentifier(string name)
    {
        // Handle reserved words and special characters
        return name switch
        {
            "class" or "function" or "return" or "var" or "let" or "const" 
                or "if" or "else" or "for" or "while" or "do" or "switch"
                or "case" or "break" or "continue" or "try" or "catch"
                or "finally" or "throw" or "new" or "this" or "super"
                or "extends" or "import" or "export" or "from" or "default"
                or "async" or "await" or "yield" or "typeof" or "instanceof"
                => $"__{name}",
            _ when name.StartsWith("@") => name.Substring(1),
            _ => ToCamelCase(name)
        };
    }

    private static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        return char.ToLowerInvariant(name[0]) + name.Substring(1);
    }

    private void Write(string text)
    {
        _output.Append(text);
    }

    private void WriteLine(string text = "")
    {
        _output.Append(text);
        _output.AppendLine();
    }

    private void Indent()
    {
        _indentLevel++;
    }

    private void Dedent()
    {
        _indentLevel--;
    }
}
