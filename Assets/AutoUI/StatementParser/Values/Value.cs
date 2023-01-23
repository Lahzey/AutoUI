using System;
using System.Text.RegularExpressions;

public abstract class Value : ParsedElement
{
    protected object[] args;

    public virtual void Prepare(object[] args)
    {
        this.args = args;
    }
    public abstract object Evaluate();
}

[ValuePattern(typeof(IdentifierToken))]
public class VariableValue : Value
{
    public override object Evaluate()
    {
        return null;
    }
    
    public string VariableName => ((IdentifierToken) args[0]).Identifier;
}

[ValuePattern(typeof(StringToken))]
public class StringValue : Value
{
    public override object Evaluate()
    {
        return ((StringToken) args[0]).Value;
    }
}

[ValuePattern(typeof(Value), @"\.", typeof(VariableValue))]
public class FieldAccessValue : Value
{
    public override object Evaluate()
    {
        return null;
    }
    
    public object Object => ((Value) args[0]).Evaluate();
    public string FieldName => ((VariableValue) args[1]).VariableName;
}

[ValuePattern(typeof(FieldAccessValue), @"=", typeof(Value))]
public class FieldAssignmentValue : Value
{
    public override object Evaluate()
    {
        object Object = ((FieldAccessValue)args[0]).Object;
        string FieldName = ((FieldAccessValue)args[0]).FieldName;
        object Value = ((Value)args[1]).Evaluate();
        return null;
    }
    
    public object Value => ((Value) args[1]).Evaluate();
}


[ValuePattern(@"\(", typeof(Value), @"\)")]
public class BracketValue : Value
{
    public override object Evaluate()
    {
        return ((Value) args[0]).Evaluate();
    }
}