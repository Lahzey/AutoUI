using System;
using System.Collections.Generic;
using System.Reflection;
using AutoUI.Data;
using AutoUI.Parsing;
using AutoUI.Parsing.Expressions;
using UnityEngine;

namespace AutoUI.Inspector {
public class CodeAreaState {
	public readonly List<string> autoCompleteOptions = new List<string>();
	public readonly List<string> autoCompleteTextInserts = new List<string>();
	public readonly List<Type> autoCompleteTypes = new List<Type>();
	public readonly List<int> lineLengths = new List<int>();

	private GUIStyle contentStyle;
	public Vector2 scrollPosition;
	public int selectionEndInternal;

	public int selectionStartInternal;
	public int SelectionStart => Mathf.Min(selectionStartInternal, selectionEndInternal);
	public int SelectionEnd => Mathf.Max(selectionStartInternal, selectionEndInternal);
	public bool ShowAutoComplete { get; private set; }
	public int AutoCompleteIndex { get; set; }
	public int AutoCompleteSelection { get; set; }

	public GUIStyle ContentStyle {
		get => contentStyle;
		set {
			contentStyle = value;
			LineHeight = ContentStyle.font.lineHeight;
			CharWidth = ContentStyle.font.characterInfo[0].advance; // doesn't matter which character, all consolas chars are the same width
		}
	}

	public int LineHeight { get; private set; }
	public int CharWidth { get; private set; }
	public string[] Lines { get; private set; }
	
	public CodeAreaTheme Theme { get; set; }

	public void SetCursor(int index) {
		selectionStartInternal = index;
		selectionEndInternal = index;
	}

	public void ClampSelection(int inputLength) {
		selectionStartInternal = Math.Clamp(selectionStartInternal, 0, inputLength);
		selectionEndInternal = Math.Clamp(selectionEndInternal, 0, inputLength);
	}

	public void SetLines(string[] lines) {
		Lines = lines;
		lineLengths.Clear();
		foreach (string line in lines) lineLengths.Add(line.Length);
	}

	public void CreateAutoComplete(string input) {
		HideAutoComplete();

		CodeInput.GetParseResult(input, parseResult => {
			int textIndex = SelectionStart;

			Type contextType = null;
			string fieldPrefix = null;

			if (textIndex > 0 && parseResult.source[textIndex - 1] == '.') {
				Expression contextExpression = GetExpressionAt(parseResult, textIndex - 2, out int expressionStart);
				if (contextExpression != null) contextType = contextExpression.GetExpectedType();
			}
			else {
				Expression previousExpression = GetExpressionAt(parseResult, textIndex - 1, out int expressionStart);
				if (previousExpression is VariableExpression variableExpression) {
					contextType = variableExpression.contextType;
					Debug.Log($"Finding field prefix from {variableExpression.VariableName} at {expressionStart} to {textIndex}");
					fieldPrefix = variableExpression.VariableName.Substring(0, textIndex - expressionStart);
				}
			}


			if (contextType == null) // null means we look at the data store
			{
				foreach (DataKeyBase dataKey in DataKeyBase.GetAll())
					if (fieldPrefix == null || (dataKey.key.StartsWith(fieldPrefix) && dataKey.key != fieldPrefix)) {
						autoCompleteOptions.Add(dataKey.key);
						autoCompleteTypes.Add(dataKey.type);
						autoCompleteTextInserts.Add(dataKey.key[(fieldPrefix?.Length ?? 0)..]);
					}

				ShowAutoComplete = true;
			}
			else if (contextType != typeof(object)) // not gonna show autocomplete for object, as this indicates that the type is unknown
			{
				foreach (FieldInfo fieldInfo in contextType.GetFields())
					if (fieldPrefix == null || (fieldInfo.Name.StartsWith(fieldPrefix) && fieldInfo.Name != fieldPrefix)) {
						autoCompleteOptions.Add(fieldInfo.Name);
						autoCompleteTypes.Add(fieldInfo.FieldType);
						autoCompleteTextInserts.Add(fieldInfo.Name[(fieldPrefix?.Length ?? 0)..]);
					}

				foreach (PropertyInfo propertyInfo in contextType.GetProperties())
					if ((propertyInfo.GetMethod.IsPublic && fieldPrefix == null) || (propertyInfo.Name.StartsWith(fieldPrefix) && propertyInfo.Name != fieldPrefix)) {
						autoCompleteOptions.Add(propertyInfo.Name);
						autoCompleteTypes.Add(propertyInfo.PropertyType);
						autoCompleteTextInserts.Add(propertyInfo.Name[(fieldPrefix?.Length ?? 0)..]);
					}

				ShowAutoComplete = autoCompleteOptions.Count > 0;
			}
		});
	}

	public void HideAutoComplete() {
		ShowAutoComplete = false;
		autoCompleteOptions.Clear();
		autoCompleteTypes.Clear();
		autoCompleteTextInserts.Clear();
		AutoCompleteIndex = 0;
	}

	private static Expression GetExpressionAt(ParseResult parseResult, int index, out int expressionStart) {
		Expression expression = parseResult.ExpressionAtPosition(index);
		if (expression != null)
			for (int i = index - 1; i >= 0; i--)
				if (parseResult.ExpressionAtPosition(i) != expression) {
					expressionStart = i + 1;
					return expression; // early return to prevent setting to start
				}

		expressionStart = 0;
		return expression;
	}

	public int GetTextIndexAtPosition(Vector2 position) {
		int lineIndex = Math.Clamp(Mathf.FloorToInt(position.y / LineHeight), 0, lineLengths.Count - 1);
		int lineLength = lineLengths[lineIndex];
		int charIndex = Math.Clamp(Mathf.RoundToInt(position.x / CharWidth), 0, lineLength);
		int textIndex = 0;
		for (int i = 0; i < lineIndex; i++) textIndex += lineLengths[i] + 1; // +1 for newline
		textIndex += charIndex;
		return textIndex;
	}

	public Vector2 GetPositionAtTextIndex(int textIndex) {
		int index = 0;
		for (int i = 0; i < lineLengths.Count; i++) {
			int lineLength = lineLengths[i];
			if (textIndex <= index + lineLength) // textIndex == index+lineLength means position is to the right of the last character on the line
			{
				int indexInLine = textIndex - index;
				return new Vector2(indexInLine * CharWidth, i * LineHeight);
			}

			index += lineLength + 1; // +1 for newline
		}

		return new Vector2(0, lineLengths.Count * LineHeight); // shouldn't happen, but default return is required
	}
}
}