using System;
using System.Collections.Generic;
using Unity.VisualScripting;

public abstract class CalculationValue : Value
{
    // private int priority;
    // private Action action;
    //
    // public override void Prepare(object[] args)
    // {
    //     base.Prepare(args);
    //
    //     Value left = (Value) args[0];
    //     Value right = (Value) args[1];
    //     if (left is CalculationValue leftCalculation)
    //     {
    //         if (leftCalculation.priority < priority)
    //         {
    //             Left = leftCalculation.Right;
    //             leftCalculation.Right = this;
    //         }
    //     }
    //     if (Right is CalculationValue rightCalculation)
    //     {
    //         if (rightCalculation.priority < priority)
    //         {
    //             Right = rightCalculation.Left;
    //             rightCalculation.Left = this;
    //         }
    //     }
    //     
    //     Action action = () => { };
    // }
    
    public static object PerformOperation(object a, object b, char operation, Value valueRef)
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
                    throw new EvaluationException($"Unsupported type for operation '{operation}': {o.GetType().FullName}", valueRef);
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

[ValuePattern(typeof(Value), @"\+", typeof(Value))]
public class AdditionValue : Value
{
    public override object Evaluate()
    {
        object left = Left.Evaluate();
        object right = Right.Evaluate();
        return CalculationValue.PerformOperation(left, right, '+', this);
    }
    
    public Value Left => (Value) args[0];
    public Value Right => (Value) args[1];
}

[ValuePattern(typeof(Value), @"-", typeof(Value))]
public class SubtractionValue : Value
{
    public override object Evaluate()
    {
        object left = Left.Evaluate();
        object right = Right.Evaluate();
        return CalculationValue.PerformOperation(left, right, '-', this);
    }
    
    public Value Left => (Value) args[0];
    public Value Right => (Value) args[1];
}

[ValuePattern(typeof(Value), @"\*", typeof(Value))]
public class MultiplicationValue : Value
{
    public override object Evaluate()
    {
        object left = Left.Evaluate();
        object right = Right.Evaluate();
        return CalculationValue.PerformOperation(left, right, '*', this);
    }
    
    public Value Left => (Value) args[0];
    public Value Right => (Value) args[1];
}

[ValuePattern(typeof(Value), @"\/", typeof(Value))]
public class DivisionValue : Value
{
    public override object Evaluate()
    {
        object left = Left.Evaluate();
        object right = Right.Evaluate();
        return CalculationValue.PerformOperation(left, right, '/', this);
    }
    
    public Value Left => (Value) args[0];
    public Value Right => (Value) args[1];
}

[ValuePattern(typeof(Value), @"%", typeof(Value))]
public class ModuloValue : Value
{
    public override object Evaluate()
    {
        object left = Left.Evaluate();
        object right = Right.Evaluate();
        return CalculationValue.PerformOperation(left, right, '%', this);
    }
    
    public Value Left => (Value) args[0];
    public Value Right => (Value) args[1];
}

[ValuePattern(typeof(Value), @"\^", typeof(Value))]
public class PowerValue : Value
{
    public override object Evaluate()
    {
        object left = Left.Evaluate();
        object right = Right.Evaluate();
        return CalculationValue.PerformOperation(left, right, '^', this);
    }
    
    public Value Left => (Value) args[0];
    public Value Right => (Value) args[1];
}