using System;
using System.Collections.Generic;
using System.Linq;

public class ValuePatternMatcher
{
    
    private static Dictionary<string, Type> ValueTypePatterns = null;
    private static Dictionary<string, int> ValueTypePatternPriorities = new Dictionary<string, int>();
    private static List<string> PatternOrder;
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
                
                // get priority of pattern from PatternPriority attribute (or 0 if not specified)
                int priority = 0;
                type.GetCustomAttributes(typeof(PatternPriorityAttribute), false).ToList().ForEach(a => priority = ((PatternPriorityAttribute) a).Priority);
                ValueTypePatternPriorities.Add(pattern, priority);
            }
        }

        PatternOrder = ValueTypePatterns.Keys.ToList();
        PatternOrder = PatternOrder.OrderByDescending(i => ValueTypePatternPriorities[i]).ToList();
    }
    
    public static Value Parse(string valueString, out ParseResult parseResult)
    {
        if (ValueTypePatterns == null) init();

        parseResult = Tokenizer.Tokenize(valueString);

        while (true)
        {
            if (!PatternMatch.MatchNext(parseResult, ValueTypePatterns, PatternOrder)) break;
        }

        if (parseResult.Count > 1 || (parseResult.Count == 1 && parseResult[0] is not Value))
        {
            ParseException exception = new ParseException(parseResult);
            bool hasUnresolvedTokens = false;
            for (int i = 0; i < parseResult.Count; i++)
            {
                if (parseResult[i] is not Value)
                {
                    exception.AddMessage("Unresolved token", parseResult.GetSourceStartIndex(i), parseResult.GetSourceEndIndex(i));
                    hasUnresolvedTokens = true;
                }
            }

            if (!hasUnresolvedTokens)
            {
                // if we only have values in the parse result, but there are more than one
                exception.AddMessage("Cannot have more than one value in a value expression", parseResult.GetSourceStartIndex(1), parseResult.Source.Length);
            }

            throw exception;
        }

        return parseResult.Count > 0 ? parseResult[0] as Value : null;
    }
    
}