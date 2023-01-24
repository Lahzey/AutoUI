using System;
using System.Text.RegularExpressions;

public abstract class Value : ParsedElement
{
    protected object[] args;

    public virtual void Prepare(object[] args)
    {
        this.args = args;
    }
    public abstract object Evaluate(DataContext context);
}

[ValuePattern(typeof(IdentifierToken))]
public class VariableValue : Value
{
    public override object Evaluate(DataContext context)
    {
        return context.Get(VariableName);
    }
    
    public string VariableName => ((IdentifierToken) args[0]).Identifier;
}

[ValuePattern(typeof(StringToken))]
public class StringValue : Value
{
    public override object Evaluate(DataContext context)
    {
        return ((StringToken) args[0]).Value;
    }
}

[ValuePattern(typeof(Value), @"\.", typeof(VariableValue))]
public class FieldAccessValue : Value
{
    public override object Evaluate(DataContext context)
    {
        object ObjectValue = Object.Evaluate(context);
        return ObjectValue.GetType().GetField(FieldName).GetValue(ObjectValue);
    }

    public Value Object => (Value) args[0];
    public string FieldName => ((VariableValue) args[1]).VariableName;
}

[ValuePattern(typeof(FieldAccessValue), @"=", typeof(Value))]
public class FieldAssignmentValue : Value
{
    public override object Evaluate(DataContext context)
    {
        object ObjectValue = ((FieldAccessValue)args[0]).Object.Evaluate(context);
        string FieldName = ((FieldAccessValue)args[0]).FieldName;
        object Value = ((Value)args[1]).Evaluate(context);
        ObjectValue.GetType().GetField(FieldName).SetValue(ObjectValue, Value);
        return Value;
    }

    public Value Value => (Value) args[1];
}


[ValuePattern(@"\(", typeof(Value), @"\)")]
public class BracketValue : Value
{
    public override object Evaluate(DataContext context)
    {
        return ((Value) args[0]).Evaluate(context);
    }
}