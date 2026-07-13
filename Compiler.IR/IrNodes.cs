using CSharpWorkers.Core;

namespace CSharpWorkers.IR;

/// <summary>
/// Intermediate representation for a class or type declaration.
/// </summary>
public class ClassDeclaration : IrNode
{
    public string Name { get; init; } = "";
    public string Namespace { get; init; } = "";
    public TypeKind Kind { get; init; }
    public List<string> BaseTypes { get; init; } = new();
    public List<FieldDeclaration> Fields { get; init; } = new();
    public List<PropertyDeclaration> Properties { get; init; } = new();
    public List<MethodDeclaration> Methods { get; init; } = new();
    public List<EventDeclaration> Events { get; init; } = new();
    public bool IsStatic { get; init; }
    public bool IsAbstract { get; init; }
    public bool IsSealed { get; init; }
    public List<TypeParameter> TypeParameters { get; init; } = new();
}

/// <summary>
/// Kind of type declaration.
/// </summary>
public enum TypeKind
{
    Class,
    Struct,
    Record,
    Interface,
    Enum
}

/// <summary>
/// Intermediate representation for a field.
/// </summary>
public class FieldDeclaration : IrNode
{
    public string Name { get; init; } = "";
    public TypeSymbol Type { get; init; } = null!;
    public Expression? Initializer { get; init; }
    public bool IsStatic { get; init; }
    public bool IsReadonly { get; init; }
    public Accessibility Accessibility { get; init; }
}

/// <summary>
/// Intermediate representation for a property.
/// </summary>
public class PropertyDeclaration : IrNode
{
    public string Name { get; init; } = "";
    public TypeSymbol Type { get; init; } = null!;
    public bool HasGetter { get; init; }
    public bool HasSetter { get; init; }
    public Expression? Initializer { get; init; }
    public bool IsStatic { get; init; }
    public bool IsVirtual { get; init; }
    public bool IsOverride { get; init; }
    public Accessibility Accessibility { get; init; }
}

/// <summary>
/// Intermediate representation for a method.
/// </summary>
public class MethodDeclaration : IrNode
{
    public string Name { get; init; } = "";
    public TypeSymbol ReturnType { get; init; } = null!;
    public List<ParameterDeclaration> Parameters { get; init; } = new();
    public BlockStatement? Body { get; init; }
    public bool IsAsync { get; init; }
    public bool IsStatic { get; init; }
    public bool IsVirtual { get; init; }
    public bool IsAbstract { get; init; }
    public bool IsOverride { get; init; }
    public bool IsExtensionMethod { get; init; }
    public List<TypeParameter> TypeParameters { get; init; } = new();
    public Accessibility Accessibility { get; init; }
}

/// <summary>
/// Intermediate representation for a method parameter.
/// </summary>
public class ParameterDeclaration : IrNode
{
    public string Name { get; init; } = "";
    public TypeSymbol Type { get; init; } = null!;
    public bool IsOptional { get; init; }
    public Expression? DefaultValue { get; init; }
    public ParameterModifier Modifier { get; init; }
}

/// <summary>
/// Parameter modifier kind.
/// </summary>
public enum ParameterModifier
{
    None,
    Ref,
    Out,
    In
}

/// <summary>
/// Intermediate representation for an event.
/// </summary>
public class EventDeclaration : IrNode
{
    public string Name { get; init; } = "";
    public TypeSymbol Type { get; init; } = null!;
    public Accessibility Accessibility { get; init; }
}

/// <summary>
/// Intermediate representation for a type parameter (generic).
/// </summary>
public class TypeParameter : IrNode
{
    public string Name { get; init; } = "";
    public List<TypeSymbol> Constraints { get; init; } = new();
    public bool HasStructConstraint { get; init; }
    public bool HasClassConstraint { get; init; }
}

/// <summary>
/// Intermediate representation for a block of statements.
/// </summary>
public class BlockStatement : IrNode
{
    public List<Statement> Statements { get; init; } = new();
}

/// <summary>
/// Base class for all statements.
/// </summary>
public abstract class Statement : IrNode { }

/// <summary>
/// Base class for all expressions.
/// </summary>
public abstract class Expression : IrNode { }

/// <summary>
/// Variable declaration statement.
/// </summary>
public class VariableDeclaration : Statement
{
    public string Name { get; init; } = "";
    public TypeSymbol Type { get; init; } = null!;
    public Expression? Initializer { get; init; }
}

/// <summary>
/// Expression statement (expression followed by semicolon).
/// </summary>
public class ExpressionStatement : Statement
{
    public Expression Expression { get; init; } = null!;
}

/// <summary>
/// Return statement.
/// </summary>
public class ReturnStatement : Statement
{
    public Expression? Value { get; init; }
}

/// <summary>
/// If statement.
/// </summary>
public class IfStatement : Statement
{
    public Expression Condition { get; init; } = null!;
    public Statement ThenBranch { get; init; } = null!;
    public Statement? ElseBranch { get; init; }
}

/// <summary>
/// While statement.
/// </summary>
public class WhileStatement : Statement
{
    public Expression Condition { get; init; } = null!;
    public Statement Body { get; init; } = null!;
}

/// <summary>
/// For statement.
/// </summary>
public class ForStatement : Statement
{
    public VariableDeclaration? Initializer { get; init; }
    public Expression? Condition { get; init; }
    public Expression? Incrementor { get; init; }
    public Statement Body { get; init; } = null!;
}

/// <summary>
/// ForEach statement.
/// </summary>
public class ForEachStatement : Statement
{
    public string VariableName { get; init; } = "";
    public TypeSymbol VariableType { get; init; } = null!;
    public Expression Collection { get; init; } = null!;
    public Statement Body { get; init; } = null!;
}

/// <summary>
/// Try-catch-finally statement.
/// </summary>
public class TryStatement : Statement
{
    public BlockStatement TryBlock { get; init; } = null!;
    public List<CatchClause> CatchClauses { get; init; } = new();
    public BlockStatement? FinallyBlock { get; init; }
}

/// <summary>
/// Catch clause.
/// </summary>
public class CatchClause : IrNode
{
    public TypeSymbol? ExceptionType { get; init; }
    public string? VariableName { get; init; }
    public BlockStatement Body { get; init; } = null!;
}

/// <summary>
/// Throw statement.
/// </summary>
public class ThrowStatement : Statement
{
    public Expression? Exception { get; init; }
}

/// <summary>
/// Binary expression (e.g., a + b).
/// </summary>
public class BinaryExpression : Expression
{
    public Expression Left { get; init; } = null!;
    public BinaryOperator Operator { get; init; }
    public Expression Right { get; init; } = null!;
}

/// <summary>
/// Binary operator kind.
/// </summary>
public enum BinaryOperator
{
    Addition, Subtraction, Multiplication, Division, Modulo,
    Equality, Inequality, LessThan, LessThanOrEqual, GreaterThan, GreaterThanOrEqual,
    LogicalAnd, LogicalOr,
    BitwiseAnd, BitwiseOr, BitwiseXor
}

/// <summary>
/// Unary expression (e.g., !a, -a).
/// </summary>
public class UnaryExpression : Expression
{
    public UnaryOperator Operator { get; init; }
    public Expression Operand { get; init; } = null!;
}

/// <summary>
/// Unary operator kind.
/// </summary>
public enum UnaryOperator
{
    Negation, LogicalNot, BitwiseNot, PrefixIncrement, PrefixDecrement,
    PostfixIncrement, PostfixDecrement
}

/// <summary>
/// Literal value expression.
/// </summary>
public class LiteralExpression : Expression
{
    public object? Value { get; init; }
    public LiteralKind Kind { get; init; }
}

/// <summary>
/// Kind of literal.
/// </summary>
public enum LiteralKind
{
    String, Boolean, Integer, Float, Null, Character
}

/// <summary>
/// Identifier reference expression.
/// </summary>
public class IdentifierExpression : Expression
{
    public string Name { get; init; } = "";
}

/// <summary>
/// Member access expression (e.g., obj.Property).
/// </summary>
public class MemberAccessExpression : Expression
{
    public Expression Target { get; init; } = null!;
    public string MemberName { get; init; } = "";
}

/// <summary>
/// Method invocation expression.
/// </summary>
public class InvocationExpression : Expression
{
    public Expression Target { get; init; } = null!;
    public string MethodName { get; init; } = "";
    public List<Expression> Arguments { get; init; } = new();
    public List<TypeSymbol> TypeArguments { get; init; } = new();
}

/// <summary>
/// Object creation expression.
/// </summary>
public class ObjectCreationExpression : Expression
{
    public TypeSymbol Type { get; init; } = null!;
    public List<Expression> Arguments { get; init; } = new();
    public List<InitializerMember> Initializers { get; init; } = new();
}

/// <summary>
/// Array creation expression.
/// </summary>
public class ArrayCreationExpression : Expression
{
    public TypeSymbol ElementType { get; init; } = null!;
    public List<Expression>? Sizes { get; init; }
    public List<Expression>? Initializers { get; init; }
}

/// <summary>
/// Anonymous object creation expression.
/// </summary>
public class AnonymousObjectCreationExpression : Expression
{
    public List<AnonymousObjectMember> Members { get; init; } = new();
}

/// <summary>
/// Anonymous object member.
/// </summary>
public class AnonymousObjectMember : IrNode
{
    public string Name { get; init; } = "";
    public Expression Value { get; init; } = null!;
}

/// <summary>
/// Initializer member (for object/collection initializers).
/// </summary>
public class InitializerMember : IrNode
{
    public string? Name { get; init; } // null for collection initializers
    public Expression Value { get; init; } = null!;
}

/// <summary>
/// Lambda expression.
/// </summary>
public class LambdaExpression : Expression
{
    public List<ParameterDeclaration> Parameters { get; init; } = new();
    public Expression? ExpressionBody { get; init; }
    public BlockStatement? BlockBody { get; init; }
}

/// <summary>
/// Conditional (ternary) expression.
/// </summary>
public class ConditionalExpression : Expression
{
    public Expression Condition { get; init; } = null!;
    public Expression WhenTrue { get; init; } = null!;
    public Expression WhenFalse { get; init; } = null!;
}

/// <summary>
/// Null coalescing expression (??).
/// </summary>
public class NullCoalescingExpression : Expression
{
    public Expression Left { get; init; } = null!;
    public Expression Right { get; init; } = null!;
}

/// <summary>
/// Null conditional expression (?.).
/// </summary>
public class NullConditionalExpression : Expression
{
    public Expression Target { get; init; } = null!;
    public string MemberName { get; init; } = "";
}

/// <summary>
/// Cast expression.
/// </summary>
public class CastExpression : Expression
{
    public TypeSymbol Type { get; init; } = null!;
    public Expression Expression { get; init; } = null!;
}

/// <summary>
/// Await expression.
/// </summary>
public class AwaitExpression : Expression
{
    public Expression InnerExpression { get; init; } = null!;
}

/// <summary>
/// Switch expression (C# 8+ pattern matching).
/// </summary>
public class SwitchExpression : Expression
{
    public Expression Input { get; init; } = null!;
    public List<SwitchArm> Arms { get; init; } = new();
}

/// <summary>
/// Switch expression arm.
/// </summary>
public class SwitchArm : IrNode
{
    public Pattern? Pattern { get; init; }
    public Expression? Guard { get; init; }
    public Expression Result { get; init; } = null!;
}

/// <summary>
/// Pattern for pattern matching.
/// </summary>
public abstract class Pattern : IrNode { }

/// <summary>
/// Constant pattern.
/// </summary>
public class ConstantPattern : Pattern
{
    public Expression Value { get; init; } = null!;
}

/// <summary>
/// Type pattern.
/// </summary>
public class TypePattern : Pattern
{
    public TypeSymbol Type { get; init; } = null!;
    public string? DesignatedVariableName { get; init; }
}

/// <summary>
/// Discard pattern (_).
/// </summary>
public class DiscardPattern : Pattern { }

/// <summary>
/// Access modifier.
/// </summary>
public enum Accessibility
{
    Public,
    Private,
    Protected,
    Internal,
    ProtectedInternal,
    PrivateProtected
}
