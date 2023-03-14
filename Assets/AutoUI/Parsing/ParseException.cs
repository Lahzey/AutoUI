using System;
using System.Collections.Generic;

namespace AutoUI.Parsing {
public class ParseException : Exception {
	private readonly List<int> endPositions = new();
	private readonly List<string> messages = new();
	private readonly List<int> startPositions = new();

	public void AddMessage(string message, int startPosition, int endPosition) {
		messages.Add(message);
		startPositions.Add(startPosition);
		endPositions.Add(endPosition);
	}

	public int MessageCount() {
		return messages.Count;
	}

	public string GetMessage(int index) {
		return messages[index];
	}

	public int GetStartPosition(int index) {
		return startPositions[index];
	}

	public int GetEndPosition(int index) {
		return endPositions[index];
	}
}
}