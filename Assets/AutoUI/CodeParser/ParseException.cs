using System;
using System.Collections.Generic;

public class ParseException : Exception
{
    private readonly ParseResult parseResult;
    private readonly List<string> messages = new List<string>();
    private readonly List<int> startPositions = new List<int>();
    private readonly List<int> endPositions = new List<int>();

    public ParseException(ParseResult parseResult)
    {
        this.parseResult = parseResult;
    }

    public ParseException(ParseResult parseResult, string message, int startPosition, int endPosition) : this(parseResult)
    {
        AddMessage(message, startPosition, endPosition);
    }
    
    public void AddMessage(string message, int startPosition, int endPosition)
    {
        messages.Add(message);
        startPositions.Add(startPosition);
        endPositions.Add(endPosition);
    }

    public int MessageCount()
    {
        return messages.Count;
    }
    
    public string GetMessage(int index)
    {
        return messages[index];
    }
    
    public int GetStartPosition(int index)
    {
        return startPositions[index];
    }
    
    public int GetEndPosition(int index)
    {
        return endPositions[index];
    }
}