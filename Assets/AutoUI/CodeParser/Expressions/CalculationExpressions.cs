using System;
using UnityEngine;

public abstract class CalculationExpression : Expression
{
    
    public static object PerformOperation(object a, object b, char operation, Expression expressionRef)
    {
        int level = 0; // 0 = short, 1 = int, 2 = long, 3 = float, 4 = double (yes, long * float = float, not double)

        foreach (object o in new object[] { a, b })
        {
            switch (o)
            {
                case short _: // short is always the lowest level, nothing to set here
                    break;
                case int _:
                    if (level < 1) level = 1;
                    break;
                case long _:
                    if (level < 2) level = 2;
                    break;
                case float _:
                    if (level < 3) level = 3;
                    break;
                case double _:
                    if (level < 4) level = 4;
                    break;
                default:
                    throw new EvaluationException($"Unsupported type for operation '{operation}': {o.GetType().FullName}", expressionRef);
            }
        }
        
        // would have really appreciated a common supertype for all numeric types to avoid this copy-paste mess
        switch (operation)
        {
            case '+':
                switch (level)
                {
                    case 0:
                        return (short) a + (short) b;
                    case 1:
                        return (int) a + (int) b;
                    case 2:
                        return (long) a + (long) b;
                    case 3:
                        return (float) a + (float) b;
                    case 4:
                        return (double) a + (double) b;
                    default:
                        throw new NotImplementedException($"Unimplemented level {level} for Mathematical operation.");
                }
            case '-':
                switch (level)
                {
                    case 0:
                        return (short) a - (short) b;
                    case 1:
                        return (int) a - (int) b;
                    case 2:
                        return (long) a - (long) b;
                    case 3:
                        return (float) a - (float) b;
                    case 4:
                        return (double) a - (double) b;
                    default:
                        throw new NotImplementedException($"Unimplemented level {level} for Mathematical operation.");
                }
            case '*':
                switch (level)
                {
                    case 0:
                        return (short) a * (short) b;
                    case 1:
                        return (int) a * (int) b;
                    case 2:
                        return (long) a * (long) b;
                    case 3:
                        return (float) a * (float) b;
                    case 4:
                        return (double) a * (double) b;
                    default:
                        throw new NotImplementedException($"Unimplemented level {level} for Mathematical operation.");
                }
            case '/':
                switch (level)
                {
                    case 0:
                        return (short) a / (short) b;
                    case 1:
                        return (int) a / (int) b;
                    case 2:
                        return (long) a / (long) b;
                    case 3:
                        return (float) a / (float) b;
                    case 4:
                        return (double) a / (double) b;
                    default:
                        throw new NotImplementedException($"Unimplemented level {level} for Mathematical operation.");
                }
            case '%':
                switch (level)
                {
                    case 0:
                        return (short) a % (short) b;
                    case 1:
                        return (int) a % (int) b;
                    case 2:
                        return (long) a % (long) b;
                    case 3:
                        return (float) a % (float) b;
                    case 4:
                        return (double) a % (double) b;
                    default:
                        throw new NotImplementedException($"Unimplemented level {level} for Mathematical operation.");
                }
            case '^':
                return Math.Pow((double) a, (double) b); // math.pow only accepts doubles, so no need to check level
            default:
                throw new NotImplementedException($"Unimplemented operator {operation} for Mathematical operation.");
        }
    }
}

[ExpressionPattern(typeof(Expression), @"\+", typeof(Expression)), PatternPriority(-1)]
public class AdditionExpression : Expression
{
    public override object Evaluate(DataContext context)
    {
        object left = Left.Evaluate(context);
        object right = Right.Evaluate(context);
        return CalculationExpression.PerformOperation(left, right, '+', this);
    }
    
    public Expression Left => (Expression) args[0];
    public Expression Right => (Expression) args[1];
}

[ExpressionPattern(typeof(Expression), @"-", typeof(Expression)), PatternPriority(-1)]
public class SubtractionExpression : Expression
{
    public override object Evaluate(DataContext context)
    {
        object left = Left.Evaluate(context);
        object right = Right.Evaluate(context);
        return CalculationExpression.PerformOperation(left, right, '-', this);
    }
    
    public Expression Left => (Expression) args[0];
    public Expression Right => (Expression) args[1];
}

[ExpressionPattern(typeof(Expression), @"\*", typeof(Expression)), PatternPriority(-1)]
public class MultiplicationExpression : Expression
{
    public override object Evaluate(DataContext context)
    {
        object left = Left.Evaluate(context);
        object right = Right.Evaluate(context);
        return CalculationExpression.PerformOperation(left, right, '*', this);
    }
    
    public Expression Left => (Expression) args[0];
    public Expression Right => (Expression) args[1];
}

[ExpressionPattern(typeof(Expression), @"\/", typeof(Expression)), PatternPriority(-1)]
public class DivisionExpression : Expression
{
    public override object Evaluate(DataContext context)
    {
        Debug.Log($"{args[0].GetType()} / {args[1].GetType()}");
        object left = Left.Evaluate(context);
        object right = Right.Evaluate(context);
        return CalculationExpression.PerformOperation(left, right, '/', this);
    }
    
    public Expression Left => (Expression) args[0];
    public Expression Right => (Expression) args[1];
}

[ExpressionPattern(typeof(Expression), @"%", typeof(Expression)), PatternPriority(-1)]
public class ModuloExpression : Expression
{
    public override object Evaluate(DataContext context)
    {
        object left = Left.Evaluate(context);
        object right = Right.Evaluate(context);
        return CalculationExpression.PerformOperation(left, right, '%', this);
    }
    
    public Expression Left => (Expression) args[0];
    public Expression Right => (Expression) args[1];
}

[ExpressionPattern(typeof(Expression), @"\^", typeof(Expression)), PatternPriority(-1)]
public class PowerExpression : Expression
{
    public override object Evaluate(DataContext context)
    {
        object left = Left.Evaluate(context);
        object right = Right.Evaluate(context);
        return CalculationExpression.PerformOperation(left, right, '^', this);
    }
    
    public Expression Left => (Expression) args[0];
    public Expression Right => (Expression) args[1];
}