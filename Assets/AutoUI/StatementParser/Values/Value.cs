using System;
using System.Reflection;
using System.Text.RegularExpressions;

public abstract class Value : ParsedElement
{
    protected object[] args;

    public virtual void Prepare(object[] args)
    {
        this.args = args;
    }

    public virtual bool CanInspect() // whether the value can be inspected in the inspector (show additional information when hovering / typing)
    {
        return false;
    }
    
    public abstract object Evaluate(DataContext context);
}

[ValuePattern(typeof(IdentifierToken)), PatternPriority(999)]
public class VariableValue : Value
{
    public override object Evaluate(DataContext context)
    {
        return context.Get(VariableName);
    }
    
    public string VariableName => ((IdentifierToken) args[0]).Identifier;
}

[ValuePattern(@"\(", typeof(Value), @"\)"), PatternPriority(999)]
public class BracketValue : Value
{
    public override object Evaluate(DataContext context)
    {
        return ((Value) args[0]).Evaluate(context);
    }
}

[ValuePattern(typeof(StringToken)), PatternPriority(999)]
public class StringValue : Value
{
    public override object Evaluate(DataContext context)
    {
        return ((StringToken) args[0]).Value;
    }
}

[ValuePattern(typeof(NumberToken)), PatternPriority(999)]
public class NumberValue : Value
{
    public override object Evaluate(DataContext context)
    {
        return ((NumberToken) args[0]).Value;
    }
}

[ValuePattern(typeof(Value), @"\?", typeof(Value), @":", typeof(Value)), PatternPriority(-999)]
public class ConditionalValue : Value
{
    public override object Evaluate(DataContext context)
    {
        object ConditionValue = Condition.Evaluate(context);
        if (ConditionValue == null) return FirstValue.Evaluate(context);
        if (ConditionValue is bool b) return b ? FirstValue.Evaluate(context) : SecondValue.Evaluate(context);
        else throw new EvaluationException("ConditionalValue condition must be a boolean", this);
    }

    public Value Condition => (Value) args[0];
    public Value FirstValue => (Value) args[1];
    public Value SecondValue => (Value) args[2];
}

[ValuePattern(typeof(Value), @"\.", typeof(VariableValue)), PatternPriority(1)]
public class FieldAccessValue : Value
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

    public Value Object => (Value) args[0];
    public string FieldName => ((VariableValue) args[1]).VariableName;
}

// probably should not support field assignment, but leaving this here if needed in the future
// [ValuePattern(typeof(FieldAccessValue), @"=", typeof(Value)), PatternPriority(-2)]
// public class FieldAssignmentValue : Value
// {
//     public override object Evaluate(DataContext context)
//     {
//         object ObjectValue = ((FieldAccessValue)args[0]).Object.Evaluate(context);
//         string FieldName = ((FieldAccessValue)args[0]).FieldName;
//         object Value = ((Value)args[1]).Evaluate(context);
//         ObjectValue.GetType().GetField(FieldName).SetValue(ObjectValue, Value);
//         return Value;
//     }
//
//     public Value Value => (Value) args[1];
// }