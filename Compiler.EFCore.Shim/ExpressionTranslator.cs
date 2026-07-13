using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Microsoft.EntityFrameworkCore
{
    /// <summary>
    /// Advanced expression tree translator for EF Core compatibility
    /// Converts C# LINQ expressions to SQL clauses at compile time
    /// </summary>
    public static class AdvancedExpressionTranslator
    {
        public static string TranslateWhere<TEntity>(Expression<Func<TEntity, bool>> predicate)
        {
            var visitor = new SqlExpressionVisitor();
            var sql = visitor.Visit(predicate.Body);
            return sql ?? "(1=1)";
        }

        public static string TranslateOrderBy<TEntity, TKey>(Expression<Func<TEntity, TKey>> keySelector, bool ascending)
        {
            if (keySelector.Body is MemberExpression memberExpr)
            {
                var columnName = GetColumnName(memberExpr.Member);
                return $"{columnName} {(ascending ? "ASC" : "DESC")}";
            }
            return "1 ASC";
        }

        public static string TranslateSelect<TEntity, TResult>(Expression<Func<TEntity, TResult>> selector)
        {
            if (selector.Body is NewExpression newExpr)
            {
                var columns = new List<string>();
                foreach (var arg in newExpr.Arguments)
                {
                    if (arg is MemberExpression memberExpr)
                    {
                        columns.Add(GetColumnName(memberExpr.Member));
                    }
                }
                return string.Join(", ", columns);
            }
            
            if (selector.Body is MemberExpression member)
            {
                return GetColumnName(member.Member);
            }
            
            return "*";
        }

        private static string GetColumnName(MemberInfo member)
        {
            // Check for Column attribute
            var columnAttr = member.GetCustomAttribute<ColumnAttribute>();
            if (columnAttr != null && !string.IsNullOrEmpty(columnAttr.Name))
            {
                return columnAttr.Name;
            }
            
            return member.Name;
        }

        private class SqlExpressionVisitor : ExpressionVisitor
        {
            private readonly StringBuilder _sql = new();
            private int _parameterIndex = 0;
            private readonly List<object> _parameters = new();

            protected override Expression VisitBinary(BinaryExpression node)
            {
                _sql.Append("(");
                Visit(node.Left);
                
                var op = node.NodeType switch
                {
                    ExpressionType.Equal => " = ",
                    ExpressionType.NotEqual => " <> ",
                    ExpressionType.GreaterThan => " > ",
                    ExpressionType.GreaterThanOrEqual => " >= ",
                    ExpressionType.LessThan => " < ",
                    ExpressionType.LessThanOrEqual => " <= ",
                    ExpressionType.AndAlso => " AND ",
                    ExpressionType.OrElse => " OR ",
                    ExpressionType.Add => " + ",
                    ExpressionType.Subtract => " - ",
                    ExpressionType.Multiply => " * ",
                    ExpressionType.Divide => " / ",
                    _ => " " + node.NodeType + " "
                };
                
                _sql.Append(op);
                Visit(node.Right);
                _sql.Append(")");
                
                return node;
            }

            protected override Expression VisitMember(MemberExpression node)
            {
                if (node.Expression is ParameterExpression)
                {
                    // This is a property of the entity
                    _sql.Append(GetColumnName(node.Member));
                }
                else if (node.Expression is ConstantExpression || node.Expression is MemberExpression)
                {
                    // This is a captured variable or constant
                    var value = EvaluateExpression(node);
                    _sql.Append(FormatValue(value));
                }
                
                return node;
            }

            protected override Expression VisitConstant(ConstantExpression node)
            {
                _sql.Append(FormatValue(node.Value));
                return node;
            }

            protected override Expression VisitMethodCall(MethodCallExpression node)
            {
                var methodName = node.Method.Name;
                
                // Handle string methods
                if (node.Object != null)
                {
                    Visit(node.Object);
                    
                    switch (methodName)
                    {
                        case "Contains":
                            _sql.Insert(_sql.Length - 1, " LIKE '%");
                            if (node.Arguments.Count > 0)
                            {
                                Visit(node.Arguments[0]);
                            }
                            _sql.Append("%'");
                            break;
                            
                        case "StartsWith":
                            _sql.Insert(_sql.Length - 1, " LIKE '");
                            if (node.Arguments.Count > 0)
                            {
                                Visit(node.Arguments[0]);
                            }
                            _sql.Append("%'");
                            break;
                            
                        case "EndsWith":
                            _sql.Insert(_sql.Length - 1, " LIKE '%");
                            if (node.Arguments.Count > 0)
                            {
                                Visit(node.Arguments[0]);
                            }
                            _sql.Append("'");
                            break;
                            
                        case "ToLower":
                            _sql.Append(" COLLATE NOCASE");
                            break;
                            
                        case "ToUpper":
                            _sql.Append(" COLLATE NOCASE");
                            break;
                    }
                }
                else if (node.Method.DeclaringType == typeof(string) && methodName == "IsNullOrEmpty")
                {
                    _sql.Append("(");
                    if (node.Arguments.Count > 0)
                    {
                        Visit(node.Arguments[0]);
                    }
                    _sql.Append(" IS NULL OR ");
                    if (node.Arguments.Count > 0)
                    {
                        Visit(node.Arguments[0]);
                    }
                    _sql.Append(" = '')");
                }
                else if (node.Method.DeclaringType == typeof(int) && methodName == "ToString")
                {
                    // CAST to text
                    var temp = new StringBuilder();
                    if (node.Object != null)
                    {
                        Visit(node.Object);
                    }
                }
                
                return node;
            }

            protected override Expression VisitUnary(UnaryExpression node)
            {
                switch (node.NodeType)
                {
                    case ExpressionType.Not:
                        _sql.Append("NOT (");
                        Visit(node.Operand);
                        _sql.Append(")");
                        break;
                        
                    case ExpressionType.Convert:
                        Visit(node.Operand);
                        break;
                        
                    default:
                        Visit(node.Operand);
                        break;
                }
                
                return node;
            }

            private string FormatValue(object? value)
            {
                if (value == null)
                    return "NULL";
                    
                if (value is bool b)
                    return b ? "1" : "0";
                    
                if (value is int || value is long || value is float || value is double || value is decimal)
                    return value.ToString() ?? "0";
                    
                if (value is DateTime dt)
                    return $"'{dt:yyyy-MM-dd HH:mm:ss}'";
                    
                if (value is Guid g)
                    return $"'{g}'";
                    
                return $"'{value.ToString()?.Replace("'", "''")}'";
            }

            private object? EvaluateExpression(Expression expr)
            {
                if (expr is ConstantExpression constExpr)
                    return constExpr.Value;
                    
                if (expr is MemberExpression memberExpr)
                {
                    if (memberExpr.Expression is ConstantExpression constExp)
                    {
                        var fieldOrProp = memberExpr.Member;
                        if (fieldOrProp is FieldInfo field)
                            return field.GetValue(constExp.Value);
                        if (fieldOrProp is PropertyInfo prop)
                            return prop.GetValue(constExp.Value);
                    }
                }
                
                return null;
            }

            public override string ToString()
            {
                return _sql.ToString();
            }
        }
    }
}
