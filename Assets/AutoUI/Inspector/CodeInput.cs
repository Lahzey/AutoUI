using System;
using System.Collections.Generic;
using System.Threading;

[Serializable]
public class CodeInput
{
    // we don't want to serialize parse results, and we also don't need to parse given string more than once
    private static readonly Dictionary<string, ParseResult> parseResults = new Dictionary<string, ParseResult>();
    
    private static readonly List<string> currentlyParsing = new List<string>();

    public string Input = "";
    public ParseResult Result => GetParseResultAwait(Input);

    /// <summary>
    /// Get the parse result for a given input string, or wait for it to be parsed if it's not already parsed.<br/>
    /// THIS WILL BLOCK NOT ONLY THE CURRENT THREAD, BUT ALSO ANY OTHER THREAD TRYING TO GET A PARSE RESULT, DO NOT USE FROM UI!
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    private static ParseResult GetParseResultAwait(string input)
    {
        lock (parseResults)
        {
            if (!parseResults.ContainsKey(input))
            {
                parseResults.Add(input, CodeParser.TryParse(input));
            }
            return parseResults[input];
        }
    }
    
    
    public static ParseResult GetParseResult(string input)
    {
        lock (parseResults)
        {
            if (parseResults.ContainsKey(input))
            {
                return parseResults[input];
            }
        }
        ParseAsync(input);
        return null;
    }

    public static void ParseAsync(string input)
    {
        lock (currentlyParsing)
        {
            if (currentlyParsing.Contains(input)) return;
            currentlyParsing.Add(input);
        }
        Thread thread = new Thread(() =>
        {
            ParseResult result = CodeParser.TryParse(input);
            lock (parseResults)
            {
                if (!parseResults.ContainsKey(input)) parseResults.Add(input, result); // the contains key check should be redundant, but just in case
            }
            lock (currentlyParsing)
            {
                currentlyParsing.Remove(input);
            }
        });
        thread.Start();
    }
}