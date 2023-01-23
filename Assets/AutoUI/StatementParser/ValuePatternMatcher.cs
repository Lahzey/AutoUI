using System;
using System.Collections.Generic;
using System.Linq;

public class ValuePatternMatcher
{
    
    private static Dictionary<string, Type> ValueTypePatterns = null;
    private static string uid = Guid.NewGuid().ToString();

    private static void init()
    {
        var subclasses =
            from assembly in AppDomain.CurrentDomain.GetAssemblies()
            from type in assembly.GetTypes()
            where type.IsSubclassOf(typeof(Value))
            select type;
        ValueTypePatterns = new Dictionary<string, Type>();
        foreach (Type type in subclasses)
        {
            // get ValuePattern attribute from type
            foreach (ValuePatternAttribute patternAttribute in type.GetCustomAttributes(typeof(ValuePatternAttribute), false))
            {
                string pattern = patternAttribute.GetPattern();
                string[] patternArray = pattern.Split(' '); 
                if (ValueTypePatterns.ContainsKey(pattern))
                    throw new ArgumentException($"Duplicate use of pattern {pattern} by {type.Name} and {ValueTypePatterns[pattern].Name}.");
                ValueTypePatterns.Add(pattern, type);
            }
        }
    }
    
    public static Value Parse(string valueString, out ParseResult parseResult)
    {
        if (ValueTypePatterns == null) init();

        parseResult = Tokenizer.Tokenize(valueString);

        while (true)
        {
            if (!PatternMatch.MatchNext(parseResult, ValueTypePatterns)) break;
        }

        if (parseResult.Elements.Count > 1 || (parseResult.Elements.Count == 1 && parseResult.Elements[0] is not Value))
        {
            ParseException exception = new ParseException();
            bool hasUnresolvedTokens = false;
            for (int i = 0; i < parseResult.Elements.Count; i++)
            {
                if (parseResult.Elements[i] is not Value)
                {
                    exception.AddMessage("Unresolved token", parseResult.SourceIndexes[i], parseResult.GetSourceEndIndex(i));
                    hasUnresolvedTokens = true;
                }
            }

            if (!hasUnresolvedTokens)
            {
                // if we only have values in the parse result, but there are more than one
                exception.AddMessage("Cannot have more than one value in a value expression", parseResult.SourceIndexes[1], parseResult.Source.Length);
            }

            throw exception;
        }

        return parseResult.Elements[0] as Value;
    }
    
}