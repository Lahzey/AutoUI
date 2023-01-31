using System;
using System.Collections.Generic;
using System.Threading;

[Serializable]
public class CodeInput
{
    // we don't want to serialize parse results, and we also don't need to parse given string more than once
    private static readonly Dictionary<string, ParseResult> parseResults = new Dictionary<string, ParseResult>();
    private static readonly Dictionary<string, ParseException> parseException = new Dictionary<string, ParseException>();
    
    public string Input;
    public bool Success;
    
    
    public static ParseResult GetParseResult(string input)
    {
        if (parseResults.ContainsKey(input))
        {
            return parseResults[input];
        }
        else
        {
            return null;
        }
    }

    public static void ParseAsync(string input)
    {
        // start new thread to parse input
        Thread thread = new Thread(() =>
        {
            ParseResult result = null;
            ParseException exception = null;
            try
            {
                // result = Parse(input);
            }
            catch (ParseException e)
            {
                exception = e;
            }
            finally
            {
                parseResults[input] = result;
                parseException[input] = exception;
            }
        });
        thread.Start();
    }
}