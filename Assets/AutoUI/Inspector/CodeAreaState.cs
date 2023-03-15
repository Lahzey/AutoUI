using System;
using System.Collections.Generic;
using System.Reflection;
using AutoUI.Data;
using AutoUI.Parsing;
using AutoUI.Parsing.Expressions;
using UnityEngine;

namespace AutoUI.Inspector {
public class CodeAreaState {
	public readonly List<string> AutoCompleteOptions = new();
	public readonly List<string> AutoCompleteTextInserts = new();
	public readonly List<Type> AutoCompleteTypes = new();
	public readonly List<int> LineLengths = new();

	private GUIStyle _contentStyle;
	public Vector2 scrollPosition;
	public int SelectionEndInternal;

	public int SelectionStartInternal;
	public int SelectionStart => Mathf.Min(SelectionStartInternal, SelectionEndInternal);
	public int SelectionEnd => Mathf.Max(SelectionStartInternal, SelectionEndInternal);
	public bool ShowAutoComplete { get; private set; }
	public int AutoCompleteIndex { get; set; }
	public int AutoCompleteSelection { get; set; }

	public GUIStyle ContentStyle {
		get => _contentStyle;
		set {
			_contentStyle = value;
			LineHeight = ContentStyle.font.lineHeight;
			CharWidth = ContentStyle.font.characterInfo[0].advance; // doesn't matter which character, all consolas chars are the same width
		}
	}

	public int LineHeight { get; private set; }
	public int CharWidth { get; private set; }
	public string[] Lines { get; private set; }
	
	public CodeAreaTheme Theme { get; set; }

	public void SetCursor(int index) {
		SelectionStartInternal = index;
		SelectionEndInternal = index;
	}

	public void ClampSelection(int inputLength) {
		SelectionStartInternal = Math.Clamp(SelectionStartInternal, 0, inputLength);
		SelectionEndInternal = Math.Clamp(SelectionEndInternal, 0, inputLength);
	}

	public void SetLines(string[] lines) {
		Lines = lines;
		LineLengths.Clear();
		foreach (string line in lines) LineLengths.Add(line.Length);
	}

	public void CreateAutoComplete(string input) {
		HideAutoComplete();

		CodeInput.GetParseResult(input, parseResult => {
			int textIndex = SelectionStart;

			Type contextType = null;
			string fieldPrefix = null;

			if (textIndex > 0 && parseResult.Source[textIndex - 1] == '.') {
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
					if (fieldPrefix == null || (dataKey.Key.StartsWith(fieldPrefix) && dataKey.Key != fieldPrefix)) {
						AutoCompleteOptions.Add(dataKey.Key);
						AutoCompleteTypes.Add(dataKey.Type);
						AutoCompleteTextInserts.Add(dataKey.Key[(fieldPrefix?.Length ?? 0)..]);
					}

				ShowAutoComplete = true;
			}
			else if (contextType != typeof(object)) // not gonna show autocomplete for object, as this indicates that the type is unknown
			{
				foreach (FieldInfo fieldInfo in contextType.GetFields())
					if (fieldPrefix == null || (fieldInfo.Name.StartsWith(fieldPrefix) && fieldInfo.Name != fieldPrefix)) {
						AutoCompleteOptions.Add(fieldInfo.Name);
						AutoCompleteTypes.Add(fieldInfo.FieldType);
						AutoCompleteTextInserts.Add(fieldInfo.Name[(fieldPrefix?.Length ?? 0)..]);
					}

				foreach (PropertyInfo propertyInfo in contextType.GetProperties())
					if ((propertyInfo.GetMethod.IsPublic && fieldPrefix == null) || (propertyInfo.Name.StartsWith(fieldPrefix) && propertyInfo.Name != fieldPrefix)) {
						AutoCompleteOptions.Add(propertyInfo.Name);
						AutoCompleteTypes.Add(propertyInfo.PropertyType);
						AutoCompleteTextInserts.Add(propertyInfo.Name[(fieldPrefix?.Length ?? 0)..]);
					}

				ShowAutoComplete = AutoCompleteOptions.Count > 0;
			}
		});
	}

	public void HideAutoComplete() {
		ShowAutoComplete = false;
		AutoCompleteOptions.Clear();
		AutoCompleteTypes.Clear();
		AutoCompleteTextInserts.Clear();
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
		int lineIndex = Math.Clamp(Mathf.FloorToInt(position.y / LineHeight), 0, LineLengths.Count - 1);
		int lineLength = LineLengths[lineIndex];
		int charIndex = Math.Clamp(Mathf.RoundToInt(position.x / CharWidth), 0, lineLength);
		int textIndex = 0;
		for (int i = 0; i < lineIndex; i++) textIndex += LineLengths[i] + 1; // +1 for newline
		textIndex += charIndex;
		return textIndex;
	}

	public Vector2 GetPositionAtTextIndex(int textIndex) {
		int index = 0;
		for (int i = 0; i < LineLengths.Count; i++) {
			int lineLength = LineLengths[i];
			if (textIndex <= index + lineLength) // textIndex == index+lineLength means position is to the right of the last character on the line
			{
				int indexInLine = textIndex - index;
				return new Vector2(indexInLine * CharWidth, i * LineHeight);
			}

			index += lineLength + 1; // +1 for newline
		}

		return new Vector2(0, LineLengths.Count * LineHeight); // shouldn't happen, but default return is required
	}
}
}