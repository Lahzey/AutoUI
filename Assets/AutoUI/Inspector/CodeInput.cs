using System;
using System.Collections.Generic;
using System.Threading;

[Serializable]
public class CodeInput
{
    // we don't want to serialize parse results, and we also don't need to parse given string more than once
    private static readonly Dictionary<string, ParseResult> parseResults = new Dictionary<string, ParseResult>();
    
    private static readonly List<string> currentlyParsing = new List<string>();

    public string Input;
    public ParseResult Result => GetParseResultAwait(Input);

    private static ParseResult GetParseResultAwait(string input)
    {
        if (!parseResults.ContainsKey(input))
        {
            parseResults.Add(input, CodeParser.TryParse(input));
        }
        return parseResults[input];
    }
    
    
    public static ParseResult GetParseResult(string input)
    {
        if (parseResults.ContainsKey(input))
        {
            return parseResults[input];
        }
        else
        {
            ParseAsync(input);
            return null;
        }
    }

    public static void ParseAsync(string input)
    {
        // start new thread to parse input
        lock (currentlyParsing)
        {
            if (currentlyParsing.Contains(input)) return;
            currentlyParsing.Add(input);
        }
        Thread thread = new Thread(() =>
        {
            parseResults.Add(input, CodeParser.TryParse(input));
            lock (currentlyParsing)
            {
                currentlyParsing.Remove(input);
            }
        });
        thread.Start();
    }
}