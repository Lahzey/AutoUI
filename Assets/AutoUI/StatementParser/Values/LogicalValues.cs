using System;

[ValuePattern(typeof(Value), @"&", typeof(Value))]
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

[ValuePattern(typeof(Value), @"&&", typeof(Value))]
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

[ValuePattern(typeof(Value), @"\|", typeof(Value))]
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

[ValuePattern(typeof(Value), @"\|\|", typeof(Value))]
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

[ValuePattern(typeof(Value), @"==", typeof(Value))]
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

[ValuePattern(typeof(Value), @"!=", typeof(Value))]
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

[ValuePattern(@"!", typeof(Value))]
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