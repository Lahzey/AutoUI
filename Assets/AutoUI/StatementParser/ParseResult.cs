using System.Collections.Generic;

public class ParseResult
{
    public readonly string Source;
    public readonly List<ParsedElement> Elements = new List<ParsedElement>();
    public readonly List<int> SourceIndexes = new List<int>(); // the start indexes of the elements in the source
    
    public ParseResult(string source)
    {
        Source = source;
        SourceIndexes.Add(0);
    }

    public void Add(ParsedElement parsedElement, int endIndex)
    {
        Elements.Add(parsedElement);
        SourceIndexes.Add(endIndex); // the exclusive end index of the element is also the start index of the next element
    }
    
    public void ReplaceRange(int startIndex, int count, ParsedElement newElement)
    {
        Elements.RemoveRange(startIndex, count);
        Elements.Insert(startIndex, newElement);
        
        // the start index of the first element and the next element after the last element are not changed
        SourceIndexes.RemoveRange(startIndex + 1, count - 1);
    }
    
    public int GetSourceEndIndex(int elementIndex)
    {
        return elementIndex == Elements.Count - 1 ? Source.Length : SourceIndexes[elementIndex + 1];
    }
}