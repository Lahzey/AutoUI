using System.Collections;
using System.Collections.Generic;
using AutoUI.Parsing.Expressions;

namespace AutoUI.Parsing {
public class ParseResult : IEnumerable<ParsedElement> {
	private readonly List<ParsedElement> Elements = new();

	public readonly Dictionary<int, Expression> ExpressionsAtPositions = new(); // a mapping used by inspector to determine which expression is at a given position
	public Expression ExpressionAtPosition(int i) => ExpressionsAtPositions.TryGetValue(i, out Expression value) ? value : null;

	// used for the inspector so it can know what certain source elements map to
	public readonly string Source;
	private readonly List<int> SourceIndexes = new(); // the start indexes of the elements in the source

	public ParseResult(string source) {
		Source = source;
		SourceIndexes.Add(0); // the first token always starts at index 0, we always add the index of the next token when adding a token
	}

	public bool Success => Exception == null;
	public Expression Expression => Success && Elements.Count == 1 ? Elements[0] as Expression : null;

	public ParseException Exception { get; private set; }

	public int Count => Elements.Count;

	public ParsedElement this[int index] => Elements[index];

	public IEnumerator<ParsedElement> GetEnumerator() {
		return Elements.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator() {
		return GetEnumerator();
	}

	public void Add(Token token, int endIndex) // can only add tokens as they are the first step in parsing, values will always replace existing tokens
	{
		Elements.Add(token);
		SourceIndexes.Add(endIndex); // the exclusive end index of the element is also the start index of the next element
	}

	public void Replace(int index, ParsedElement newElement) {
		ReplaceRange(index, 1, newElement);
	}

	public void ReplaceRange(int startIndex, int count, ParsedElement newElement) {
		Elements.RemoveRange(startIndex, count);
		Elements.Insert(startIndex, newElement);

		// the start index of the first element and the next element after the last element are not changed
		SourceIndexes.RemoveRange(startIndex + 1, count - 1);

		// storing the positions of values index by index like this is inefficient, but only has to be done on parse and should save time later
		if (newElement is Expression value)
			for (int i = SourceIndexes[startIndex]; i < SourceIndexes[startIndex + 1]; i++)
				if (value.HidesInspectedChildren() || !ExpressionsAtPositions.ContainsKey(i))
					ExpressionsAtPositions[i] = value; // never overwrite existing values, we want the lowest level expression to take priority
	}

	public void AddExceptionMessage(string message, int startPosition, int endPosition) {
		if (Exception == null) Exception = new ParseException();

		Exception.AddMessage(message, startPosition, endPosition);
	}

	public int GetSourceStartIndex(int elementIndex) {
		return SourceIndexes[elementIndex];
	}

	public int GetSourceEndIndex(int elementIndex) {
		return elementIndex == Elements.Count - 1 ? Source.Length : SourceIndexes[elementIndex + 1];
	}
}
}