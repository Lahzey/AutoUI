using System;

[ExpressionPattern(typeof(Expression), @"==", typeof(Expression)), PatternPriority(-100)]
public class EqualsExpression : Expression
{
    public override object Evaluate(DataContext context)
    {
        object left = Left.Evaluate(context);
        object right = Right.Evaluate(context);
        return left == right;
    }
    
    public Expression Left => (Expression) args[0];
    public Expression Right => (Expression) args[1];
}

[ExpressionPattern(typeof(Expression), @"!=", typeof(Expression)), PatternPriority(-100)]
public class NotEqualsExpression : Expression
{
    public override object Evaluate(DataContext context)
    {
        object left = Left.Evaluate(context);
        object right = Right.Evaluate(context);
        return left != right;
    }
    
    public Expression Left => (Expression) args[0];
    public Expression Right => (Expression) args[1];
}

[ExpressionPattern(typeof(Expression), @">", typeof(Expression)), PatternPriority(-100)]
public class GreaterThanExpression : Expression
{
    public override object Evaluate(DataContext context)
    {
        object left = Left.Evaluate(context);
        object right = Right.Evaluate(context);
        if (left is short or int or long or float or double && right is short or int or long or float or double)
        {
            return Convert.ToDouble(left) > Convert.ToDouble(right);
        } else throw new EvaluationException("Cannot perform > on non-numeric values", this);
    }
    
    public Expression Left => (Expression) args[0];
    public Expression Right => (Expression) args[1];
}

[ExpressionPattern(typeof(Expression), @">=", typeof(Expression)), PatternPriority(-100)]
public class GreaterThanOrEqualExpression : Expression
{
    public override object Evaluate(DataContext context)
    {
        object left = Left.Evaluate(context);
        object right = Right.Evaluate(context);
        if (left is short or int or long or float or double && right is short or int or long or float or double)
        {
            return Convert.ToDouble(left) >= Convert.ToDouble(right);
        } else throw new EvaluationException("Cannot perform >= on non-numeric values", this);
    }
    
    public Expression Left => (Expression) args[0];
    public Expression Right => (Expression) args[1];
}

[ExpressionPattern(typeof(Expression), @"<", typeof(Expression)), PatternPriority(-100)]
public class SmallerThanExpression : Expression
{
    public override object Evaluate(DataContext context)
    {
        object left = Left.Evaluate(context);
        object right = Right.Evaluate(context);
        if (left is short or int or long or float or double && right is short or int or long or float or double)
        {
            return Convert.ToDouble(left) < Convert.ToDouble(right);
        } else throw new EvaluationException("Cannot perform < on non-numeric values", this);
    }
    
    public Expression Left => (Expression) args[0];
    public Expression Right => (Expression) args[1];
}

[ExpressionPattern(typeof(Expression), @"<=", typeof(Expression)), PatternPriority(-100)]
public class SmallerThanOrEqualExpression : Expression
{
    public override object Evaluate(DataContext context)
    {
        object left = Left.Evaluate(context);
        object right = Right.Evaluate(context);
        if (left is short or int or long or float or double && right is short or int or long or float or double)
        {
            return Convert.ToDouble(left) <= Convert.ToDouble(right);
        } else throw new EvaluationException("Cannot perform <= on non-numeric values", this);
    }
    
    public Expression Left => (Expression) args[0];
    public Expression Right => (Expression) args[1];
}

[ExpressionPattern(typeof(Expression), @"&", typeof(Expression)), PatternPriority(-101)]
public class AndExpression : Expression
{
    public override object Evaluate(DataContext context)
    {
        object left = Left.Evaluate(context);
        object right = Right.Evaluate(context);
        if (!(left is bool) || !(right is bool))
            throw new EvaluationException("Cannot perform AND on non-boolean values", this);
        return (bool) left & (bool) right;
    }
    
    public Expression Left => (Expression) args[0];
    public Expression Right => (Expression) args[1];
}

[ExpressionPattern(typeof(Expression), @"&&", typeof(Expression)), PatternPriority(-101)]
public class LogicalAndExpression : Expression
{
    public override object Evaluate(DataContext context)
    {
        object left = Left.Evaluate(context);
        object right = Right.Evaluate(context);
        if (!(left is bool) || !(right is bool))
            throw new EvaluationException("Cannot perform AND on non-boolean values", this);
        return (bool) left && (bool) right;
    }
    
    public Expression Left => (Expression) args[0];
    public Expression Right => (Expression) args[1];
}

[ExpressionPattern(typeof(Expression), @"\|", typeof(Expression)), PatternPriority(-102)]
public class OrExpression : Expression
{
    public override object Evaluate(DataContext context)
    {
        object left = Left.Evaluate(context);
        object right = Right.Evaluate(context);
        if (!(left is bool) || !(right is bool))
            throw new EvaluationException("Cannot perform OR on non-boolean values", this);
        return (bool) left | (bool) right;
    }
    
    public Expression Left => (Expression) args[0];
    public Expression Right => (Expression) args[1];
}

[ExpressionPattern(typeof(Expression), @"\|\|", typeof(Expression)), PatternPriority(-102)]
public class LogicalOrExpression : Expression
{
    public override object Evaluate(DataContext context)
    {
        object left = Left.Evaluate(context);
        object right = Right.Evaluate(context);
        if (!(left is bool) || !(right is bool))
            throw new EvaluationException("Cannot perform OR on non-boolean values", this);
        return (bool) left || (bool) right;
    }
    
    public Expression Left => (Expression) args[0];
    public Expression Right => (Expression) args[1];
}

[ExpressionPattern(@"!", typeof(Expression)), PatternPriority(-103)]
public class NotExpression : Expression
{
    public override object Evaluate(DataContext context)
    {
        object value = Expression.Evaluate(context);
        if (!(value is bool))
            throw new EvaluationException("Cannot perform NOT on non-boolean values", this);
        return !(bool)value;
    }

    public Expression Expression => (Expression) args[0];
}