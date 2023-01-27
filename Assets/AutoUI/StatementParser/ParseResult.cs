using System.Collections;
using System.Collections.Generic;

public class ParseResult : IEnumerable<ParsedElement>
{
    // used for the inspector so it can know what certain source elements map to
    public readonly string Source;
    public readonly Dictionary<int, Value> ValuesAtPositions = new Dictionary<int, Value>(); // a mapping used by inspector to determine which value is at a given position

    private readonly List<ParsedElement> Elements = new List<ParsedElement>();
    private readonly List<int> SourceIndexes = new List<int>(); // the start indexes of the elements in the source
    
    public ParseResult(string source)
    {
        Source = source;
        SourceIndexes.Add(0); // the first token always starts at index 0, we always add the index of the next token when adding a token
    }

    public void Add(Token token, int endIndex) // can only add tokens as they are the first step in parsing, values will always replace existing tokens
    {
        Elements.Add(token);
        SourceIndexes.Add(endIndex); // the exclusive end index of the element is also the start index of the next element
    }
    
    public void Replace(int index, ParsedElement newElement)
    {
        ReplaceRange(index, 1, newElement);
    }
    
    public void ReplaceRange(int startIndex, int count, ParsedElement newElement)
    {
        Elements.RemoveRange(startIndex, count);
        Elements.Insert(startIndex, newElement);
        
        // the start index of the first element and the next element after the last element are not changed
        SourceIndexes.RemoveRange(startIndex + 1, count - 1);
        
        // storing the positions of values index by index like this is inefficient, but only has to be done on parse and should save time later
        if (newElement is Value value && value.CanInspect())
        {
            for (int i = SourceIndexes[startIndex]; i < SourceIndexes[startIndex + 1]; i++)
                if (!ValuesAtPositions.ContainsKey(i)) ValuesAtPositions[i] = value; // never overwrite existing values, we want the lowest level value to take priority
        }
    }
    
    public int GetSourceStartIndex(int elementIndex)
    {
        return SourceIndexes[elementIndex];
    }
    
    public int GetSourceEndIndex(int elementIndex)
    {
        return elementIndex == Elements.Count - 1 ? Source.Length : SourceIndexes[elementIndex + 1];
    }
    
    public int Count => Elements.Count;
    
    public ParsedElement this[int index] => Elements[index];

    public IEnumerator<ParsedElement> GetEnumerator()
    {
        return Elements.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}