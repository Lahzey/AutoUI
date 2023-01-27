using System;

[ValuePattern(typeof(Value), @"==", typeof(Value)), PatternPriority(-100)]
public class EqualsValue : Value
{
    public override object Evaluate(DataContext context)
    {
        object left = Left.Evaluate(context);
        object right = Right.Evaluate(context);
        return left == right;
    }
    
    public Value Left => (Value) args[0];
    public Value Right => (Value) args[1];
}

[ValuePattern(typeof(Value), @"!=", typeof(Value)), PatternPriority(-100)]
public class NotEqualsValue : Value
{
    public override object Evaluate(DataContext context)
    {
        object left = Left.Evaluate(context);
        object right = Right.Evaluate(context);
        return left != right;
    }
    
    public Value Left => (Value) args[0];
    public Value Right => (Value) args[1];
}

[ValuePattern(typeof(Value), @">", typeof(Value)), PatternPriority(-100)]
public class GreaterThanValue : Value
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
    
    public Value Left => (Value) args[0];
    public Value Right => (Value) args[1];
}

[ValuePattern(typeof(Value), @">=", typeof(Value)), PatternPriority(-100)]
public class GreaterThanOrEqualValue : Value
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
    
    public Value Left => (Value) args[0];
    public Value Right => (Value) args[1];
}

[ValuePattern(typeof(Value), @"<", typeof(Value)), PatternPriority(-100)]
public class SmallerThanValue : Value
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
    
    public Value Left => (Value) args[0];
    public Value Right => (Value) args[1];
}

[ValuePattern(typeof(Value), @"<=", typeof(Value)), PatternPriority(-100)]
public class SmallerThanOrEqualValue : Value
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
    
    public Value Left => (Value) args[0];
    public Value Right => (Value) args[1];
}

[ValuePattern(typeof(Value), @"&", typeof(Value)), PatternPriority(-101)]
public class AndValue : Value
{
    public override object Evaluate(DataContext context)
    {
        object left = Left.Evaluate(context);
        object right = Right.Evaluate(context);
        if (!(left is bool) || !(right is bool))
            throw new EvaluationException("Cannot perform AND on non-boolean values", this);
        return (bool) left & (bool) right;
    }
    
    public Value Left => (Value) args[0];
    public Value Right => (Value) args[1];
}

[ValuePattern(typeof(Value), @"&&", typeof(Value)), PatternPriority(-101)]
public class LogicalAndValue : Value
{
    public override object Evaluate(DataContext context)
    {
        object left = Left.Evaluate(context);
        object right = Right.Evaluate(context);
        if (!(left is bool) || !(right is bool))
            throw new EvaluationException("Cannot perform AND on non-boolean values", this);
        return (bool) left && (bool) right;
    }
    
    public Value Left => (Value) args[0];
    public Value Right => (Value) args[1];
}

[ValuePattern(typeof(Value), @"\|", typeof(Value)), PatternPriority(-102)]
public class OrValue : Value
{
    public override object Evaluate(DataContext context)
    {
        object left = Left.Evaluate(context);
        object right = Right.Evaluate(context);
        if (!(left is bool) || !(right is bool))
            throw new EvaluationException("Cannot perform OR on non-boolean values", this);
        return (bool) left | (bool) right;
    }
    
    public Value Left => (Value) args[0];
    public Value Right => (Value) args[1];
}

[ValuePattern(typeof(Value), @"\|\|", typeof(Value)), PatternPriority(-102)]
public class LogicalOrValue : Value
{
    public override object Evaluate(DataContext context)
    {
        object left = Left.Evaluate(context);
        object right = Right.Evaluate(context);
        if (!(left is bool) || !(right is bool))
            throw new EvaluationException("Cannot perform OR on non-boolean values", this);
        return (bool) left || (bool) right;
    }
    
    public Value Left => (Value) args[0];
    public Value Right => (Value) args[1];
}

[ValuePattern(@"!", typeof(Value)), PatternPriority(-103)]
public class NotValue : Value
{
    public override object Evaluate(DataContext context)
    {
        object value = Value.Evaluate(context);
        if (!(value is bool))
            throw new EvaluationException("Cannot perform NOT on non-boolean values", this);
        return !(bool)value;
    }

    public Value Value => (Value) args[0];
}