using System;
using System.Reflection;
using System.Text.RegularExpressions;

public abstract class Expression : ParsedElement
{
    protected object[] args;

    public virtual void Prepare(object[] args)
    {
        this.args = args;
    }

    public virtual bool CanInspect() // whether the expression can be inspected in the inspector (show additional information when hovering / typing)
    {
        return false;
    }
    
    public abstract object Evaluate(DataContext context);
}

[ExpressionPattern(typeof(IdentifierToken)), PatternPriority(999)]
public class VariableExpression : Expression
{
    public override object Evaluate(DataContext context)
    {
        return context.Get(VariableName);
    }
    
    public string VariableName => ((IdentifierToken) args[0]).Identifier;
}

[ExpressionPattern(@"\(", typeof(Expression), @"\)"), PatternPriority(999)]
public class BracketExpression : Expression
{
    public override object Evaluate(DataContext context)
    {
        return ((Expression) args[0]).Evaluate(context);
    }
}

[ExpressionPattern(typeof(StringToken)), PatternPriority(999)]
public class StringExpression : Expression
{
    public override object Evaluate(DataContext context)
    {
        return ((StringToken) args[0]).Value;
    }
}

[ExpressionPattern(typeof(NumberToken)), PatternPriority(999)]
public class NumberExpression : Expression
{
    public override object Evaluate(DataContext context)
    {
        return ((NumberToken) args[0]).Value;
    }
}

[ExpressionPattern(typeof(Expression), @"\?", typeof(Expression), @":", typeof(Expression)), PatternPriority(-999)]
public class ConditionalExpression : Expression
{
    public override object Evaluate(DataContext context)
    {
        object ConditionValue = Condition.Evaluate(context);
        if (ConditionValue == null) return FirstExpression.Evaluate(context);
        if (ConditionValue is bool b) return b ? FirstExpression.Evaluate(context) : SecondExpression.Evaluate(context);
        else throw new EvaluationException("ConditionalExpression condition must be a boolean", this);
    }

    public Expression Condition => (Expression) args[0];
    public Expression FirstExpression => (Expression) args[1];
    public Expression SecondExpression => (Expression) args[2];
}

[ExpressionPattern(typeof(Expression), @"\.", typeof(VariableExpression)), PatternPriority(1)]
public class FieldAccessExpression : Expression
{
    public override object Evaluate(DataContext context)
    {
        object ObjectValue = Object.Evaluate(context);
        
        if(ObjectValue == null) throw new EvaluationException("Trying to access field of null object", this);
        Type type = ObjectValue.GetType();
        
        FieldInfo fieldInfo = type.GetField(FieldName);
        if (fieldInfo != null) return fieldInfo.GetValue(ObjectValue);
        PropertyInfo propertyInfo = type.GetProperty(FieldName);
        if (propertyInfo != null) return propertyInfo.GetValue(ObjectValue);
        else throw new EvaluationException($"Field or property '{FieldName}' not found for type '{type.FullName}'", this);
    }

    public Expression Object => (Expression) args[0];
    public string FieldName => ((VariableExpression) args[1]).VariableName;
}

// probably should not support field assignment, but leaving this here if needed in the future
// [ValuePattern(typeof(FieldAccessExpression), @"=", typeof(Expression)), PatternPriority(-2)]
// public class FieldAssignmentValue : Expression
// {
//     public override object Evaluate(DataContext context)
//     {
//         object ObjectValue = ((FieldAccessExpression)args[0]).Object.Evaluate(context);
//         string FieldName = ((FieldAccessExpression)args[0]).FieldName;
//         object Expression = ((Expression)args[1]).Evaluate(context);
//         ObjectValue.GetType().GetField(FieldName).SetValue(ObjectValue, Expression);
//         return Expression;
//     }
//
//     public Expression Expression => (Expression) args[1];
// }