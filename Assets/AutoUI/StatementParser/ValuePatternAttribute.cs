using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

[System.AttributeUsage(System.AttributeTargets.Class)]
public class ValuePatternAttribute : System.Attribute
{
    private readonly object[] patternParts;

    public ValuePatternAttribute(params object[] patternParts)
    {
        this.patternParts = patternParts;
    }

    public string GetPattern()
    {
        List<string> pattern = new List<string>();
        foreach (object patternPart in patternParts)
        {
            if (patternPart is Type) pattern.Add(TypeOfPattern((Type) patternPart));
            else if (patternPart is string) pattern.Add((string)patternPart);
            else throw new ArgumentException("ValuePatternAttribute only accepts strings and types as arguments.");
        }

        return string.Join(" ", pattern);
    }

    private static string TypeOfPattern(Type type)
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