using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

[System.AttributeUsage(System.AttributeTargets.Class)]
public class ExpressionPatternAttribute : System.Attribute
{
    public bool AutoSpace { get; set; } = true;
    private readonly object[] patternParts;

    public ExpressionPatternAttribute(params object[] patternParts)
    {
        this.patternParts = patternParts;
    }

    public string GetPattern()
    {
        List<string> pattern = new List<string>();
        foreach (object patternPart in patternParts)
        {
            if (patternPart is Type type) pattern.Add(TypeOfPattern(type));
            else if (patternPart is string text) pattern.Add(text);
            else throw new ArgumentException("ExpressionPatternAttribute only accepts strings and types as arguments.");
        }

        return string.Join(AutoSpace ? " " : "", pattern);
    }

    public static string TypeOfPattern(Type type)
    {
        // return a regex pattern that matches the type plus any additional type names after a trailing '>' that would denote a subtype
        return Regex.Escape(TypeOfPlaceholder(type)) + @"(?:>[a-zA-Z0-9_]+)?";
    }
    
    public static string TypeOfPlaceholder(Type type)
    {
        string typeHierarchy = type.FullName;
        while (type.BaseType != null)
        {
            type = type.BaseType;
            typeHierarchy = type.FullName + ">" + typeHierarchy;
        }

        return "$" + typeHierarchy;
    }
}

[System.AttributeUsage(System.AttributeTargets.Class)]
public class PatternPriorityAttribute : System.Attribute
{
    public readonly int Priority;
    
    public PatternPriorityAttribute(int priority)
    {
        Priority = priority;
    }
}
