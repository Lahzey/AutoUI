﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

public class PatternMatch
{

    public static bool MatchNext(ParseResult parseResult, Dictionary<string, Type> valueTypePatterns, List<string> patternOrder)
    {
        // creates a string list of with a placeholder of each element and the joins them into one string
        string elementsString = string.Join(' ', parseResult.Select(m => m.GetPlaceholder()));
        
        // use regex to find matches of all possible patterns and keep the one matching the most elements
        Match match = null;
        Type matchedType = null;
        foreach (string valueTypePattern in patternOrder)
        {
            // find each match of valueTypePattern in placeholders using Regex
            Match currentMatch = Regex.Match(elementsString, valueTypePattern);
            if (currentMatch.Success)
            {
                match = currentMatch;
                matchedType = valueTypePatterns[valueTypePattern];
                break;
            }
        }
        
        if (match == null) return false; // no matches found
        
        // create a new PatternMatch object with the best match
        PatternMatch patternMatch = new PatternMatch(match, parseResult, elementsString);
        
        // replaces the matched elements with a new instance of the matched type
        patternMatch.ReplaceMatchedElements(matchedType);

        return true;
    }
    
    private readonly Match match;
    private readonly ParseResult parseResult;
    private readonly string elementsString; // a string representation of the ParseResult elements with spaces between them

    private List<object> matchedElements; // can contain either ParsedElements or Lists of them
    private int firstMatchedElementIndex;
    private int currentMatchedElementIndex;
    private int currentStringIndex;

    private PatternMatch(Match match, ParseResult parseResult, string elementsString)
    {
        this.match = match;
        this.parseResult = parseResult;
        this.elementsString = elementsString;
        
        currentStringIndex = match.Groups[0].Index;
        
        // counting the amount of spaces before currentStringIndex will point us to the first ParsedElements within the match
        firstMatchedElementIndex = 0;
        for (int i = 0; i < currentStringIndex; i++) if (elementsString[i] == ' ') firstMatchedElementIndex++;
        currentMatchedElementIndex = firstMatchedElementIndex;
        
        matchedElements = MatchGroup(0);
    }

    private void ReplaceMatchedElements(Type matchedType)
    {
        Value value = (Value) Activator.CreateInstance(matchedType);
        value.Prepare(matchedElements.ToArray());
        parseResult.ReplaceRange(firstMatchedElementIndex, currentMatchedElementIndex - firstMatchedElementIndex, value);
    }

    private List<object> MatchGroup(int groupIndex)
    {
        List<object> matchedElements = new List<object>();
        Group group = match.Groups[groupIndex];
        Group nextGroup = match.Groups.Count > groupIndex + 1 ? match.Groups[groupIndex + 1] : null;
        int groupEnd = group.Index + group.Length;
        while (currentStringIndex < groupEnd)
        {
            if (nextGroup?.Index == currentStringIndex)
            {
                // this next group is nested inside this group, let it handle its part of the string and add all its matched elements to this group as one list
                matchedElements.Add(MatchGroup(groupIndex + 1));
            }
            else
            {
                // step forward in the elementsString until we reach the end of the group or next space, indicating the end of the current element
                while (currentStringIndex < groupEnd)
                {
                    currentStringIndex++;
                    if (elementsString[currentStringIndex - 1] == ' ') break; // if we just encountered a space, we are done with this element
                }

                ParsedElement parsedElement = parseResult[currentMatchedElementIndex];
                if (parsedElement is not SingleCharToken or MultiCharToken) // no need to add these as they are already contained as is in the pattern and do not carry any additional information
                {
                    matchedElements.Add(parsedElement);
                }
                currentMatchedElementIndex++;
            }
        }

        return matchedElements;
    }
}