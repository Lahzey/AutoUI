using System;
using System.Collections.Generic;
using System.Linq;
using AutoUI.Parsing;
using AutoUI.Parsing.Expressions;
using UnityEditor;
using UnityEngine;

namespace AutoUI.Inspector {
public class CodeArea {
	private static readonly string EnglishCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!\"#$%&'()*+,-./:;<=>?@[\\]^_`{|}~ \t\n\r";
	private static readonly Dictionary<int, Font> ConsolasFonts = new(); // <size, font>

	private static readonly Vector2 Padding = new(5, 2); // left, top and right, bottom both have this padding
	public static readonly int MaxAutoCompleteOptions = 5;

	private static readonly GUIStyle CodeSegmentStyle;

	static CodeArea() {
		CodeSegmentStyle = new GUIStyle(GUI.skin.label);
		CodeSegmentStyle.margin = new RectOffset(0, 0, 0, 0);
		CodeSegmentStyle.border = new RectOffset(0, 0, 0, 0);
		CodeSegmentStyle.padding = new RectOffset(0, 0, 0, 0);
		CodeSegmentStyle.alignment = TextAnchor.UpperLeft;
	}

	public static string ScrolledArea(Rect position, string input, ParseResult parseResult, GUIStyle style, out Vector2 preferredSize) {
		int controlId = GUIUtility.GetControlID(FocusType.Keyboard);
		bool hasFocus = GUIUtility.keyboardControl == controlId;
		CodeAreaState state = (CodeAreaState)GUIUtility.GetStateObject(typeof(CodeAreaState), controlId);

		CodeSegmentStyle.font = GetConsolasFont(style.fontSize > 0 ? style.fontSize : style.font.fontSize);
		CodeSegmentStyle.fontSize = CodeSegmentStyle.font.fontSize; // just to be sure
		state.ContentStyle = CodeSegmentStyle;
		state.Theme = EditorGUIUtility.isProSkin ? CodeAreaTheme.DEFAULT_DARK : CodeAreaTheme.DEFAULT_LIGHT;
		state.SetLines(input.Split('\n'));

		Rect inputBounds = new(position.position + Padding, position.size - Padding * 2);
		switch (Event.current.GetTypeForControl(controlId)) {
			case EventType.KeyDown:
				string newInput = HandleKeyDownEvent(input, state, out bool canShowAutoComplete);
				if (newInput != input) {
					if (canShowAutoComplete) state.CreateAutoComplete(newInput);
					else state.HideAutoComplete();
					GUI.changed = true;
				}

				input = newInput;
				break;
			case EventType.MouseDown:
				if (!hasFocus && inputBounds.Contains(Event.current.mousePosition))
					GUIUtility.keyboardControl = controlId;
				else if (hasFocus && !inputBounds.Contains(Event.current.mousePosition)) GUIUtility.keyboardControl = 0;

				int clickedIndex = state.GetTextIndexAtPosition(Event.current.mousePosition - inputBounds.position);
				if (Event.current.shift) state.SelectionEndInternal = clickedIndex;
				else state.SetCursor(clickedIndex);
				Event.current.Use();
				state.HideAutoComplete();
				break;
			case EventType.MouseDrag:
				state.SelectionEndInternal = state.GetTextIndexAtPosition(Event.current.mousePosition - inputBounds.position);
				Event.current.Use();
				state.HideAutoComplete();
				break;
			case EventType.Repaint:
				Draw(position, input, parseResult, hasFocus, state);
				break;
		}

		preferredSize = GetCodeAreaSize(state, position.width);

		return input;
	}

	private static void Draw(Rect position, string input, ParseResult parseResult, bool hasFocus, CodeAreaState state) {
		Color oldColor = GUI.color;
		GUIDrawUtils.FillRoundRect(position, state.Theme.BackgroundColor, 5);
		GUIDrawUtils.DrawRoundRect(position, hasFocus ? state.Theme.SelectedBorderColor : state.Theme.NormalBorderColor, 2, 5);
		Rect codeAreaBounds = new(Vector2.zero, GetCodeAreaSize(state, position.width));
		state.scrollPosition = GUI.BeginScrollView(position, state.scrollPosition, codeAreaBounds);
		Rect contentBounds = new(codeAreaBounds.position + Padding, codeAreaBounds.size - Padding * 2);
		GUI.BeginGroup(contentBounds);

		EditorGUIUtility.AddCursorRect(new Rect(Vector2.zero, contentBounds.size), MouseCursor.Text);

		#region Selection

		if (hasFocus) {
			Vector2 cursorPosition = state.GetPositionAtTextIndex(state.SelectionEndInternal);
			
			// draw a vertical line for the cursor
			GUIDrawUtils.FillRoundRect(new Rect(cursorPosition, new Vector2(1, state.LineHeight)), state.Theme.CursorColor, 1);
			
			// if there is a selection, draw a colored rectangle over it (text is drawn later, so it acts as a background)
			if (state.SelectionStart != state.SelectionEnd) {
				string selection = input.Substring(state.SelectionStart, state.SelectionEnd - state.SelectionStart);
				string[] lines = selection.Split('\n');
				int index = state.SelectionStart;
				GUI.color = state.Theme.SelectionTextBackgroundColor;
				foreach (string line in lines) {
					Vector2 selectionStartPosition = state.GetPositionAtTextIndex(index);
					Vector2 selectionEndPosition = state.GetPositionAtTextIndex(index + line.Length);
					GUI.DrawTexture(new Rect(selectionStartPosition, new Vector2(selectionEndPosition.x - selectionStartPosition.x, state.LineHeight)), EditorGUIUtility.whiteTexture);
					index += line.Length + 1;
				}
			}
		}

		#endregion

		#region Errors

		if (parseResult != null && parseResult.Exception != null)
			for (int i = 0; i < parseResult.Exception.MessageCount(); i++) {
				Debug.LogWarning("Error in Code Input Field: " + parseResult.Exception.GetMessage(i));
				GUIDrawUtils.DrawSquigglyLine(parseResult.Exception.GetStartPosition(i), parseResult.Exception.GetEndPosition(i), state);
			}

		#endregion

		#region CodeElements

		int charCountBeforeLine = 0;
		for (int lineIndex = 0; lineIndex < state.Lines.Length; lineIndex++) {
			Expression currentExpression = null;
			int currentExpressionStart = 0;
			int y = lineIndex * state.LineHeight;
			for (int indexInLine = 0; indexInLine < state.LineLengths[lineIndex]; indexInLine++) {
				Expression expression = null;
				parseResult?.ExpressionsAtPositions?.TryGetValue(indexInLine, out expression);

				if (expression != currentExpression) {
					if (currentExpressionStart < indexInLine)
						// finish up current expression
						StylizedTextField(new Vector2(currentExpressionStart * state.CharWidth, y), state.Lines[lineIndex].Substring(currentExpressionStart, indexInLine - currentExpressionStart), currentExpression, state);

					currentExpression = expression;
					currentExpressionStart = indexInLine;
				}
			}

			StylizedTextField(new Vector2(currentExpressionStart * state.CharWidth, y), state.Lines[lineIndex].Substring(currentExpressionStart, state.LineLengths[lineIndex] - currentExpressionStart), currentExpression, state);
			charCountBeforeLine += state.LineLengths[lineIndex] + 1; // +1 for the newline character
		}

		#endregion


		GUI.EndGroup();
		GUI.EndScrollView();

		#region AutoComplete

		if (hasFocus && state.ShowAutoComplete) {
			int longestFieldName = state.AutoCompleteOptions.Skip(state.AutoCompleteIndex).Take(MaxAutoCompleteOptions).Aggregate(0, (current, fieldName) => Mathf.Max(current, fieldName.Length));
			int longestTypeName = state.AutoCompleteTypes.Skip(state.AutoCompleteIndex).Take(MaxAutoCompleteOptions).Aggregate(0, (current, fieldType) => Mathf.Max(current, fieldType.Name.Length));

			Vector2 positionAtCursor = position.position + contentBounds.position + state.GetPositionAtTextIndex(state.SelectionEndInternal);
			Vector2 autoCompleteWindowSize = new((longestFieldName + 3 + longestTypeName) * state.CharWidth, Math.Min(MaxAutoCompleteOptions, state.AutoCompleteOptions.Count) * state.LineHeight);
			float maxY = position.y + position.height;
			float maxWindowY = positionAtCursor.y + autoCompleteWindowSize.y;
			if (maxWindowY > maxY) positionAtCursor.y -= maxWindowY - maxY;
			Rect bounds = new(positionAtCursor, autoCompleteWindowSize);

			GUI.BeginClip(bounds);
			bounds.position = Vector2.zero;
			GUIDrawUtils.FillRoundRect(bounds, state.Theme.AutoCompleteNormalBackgroundColor, 0);
			GUIDrawUtils.DrawRoundRect(bounds, state.Theme.AutoCompleteBorderColor, 1, 0);

			int endIndex = Math.Min(state.AutoCompleteIndex + MaxAutoCompleteOptions, state.AutoCompleteOptions.Count);
			for (int i = state.AutoCompleteIndex; i < endIndex; i++) {
				string fieldName = state.AutoCompleteOptions[i];
				string fieldType = state.AutoCompleteTypes[i].Name;
				float y = bounds.y + (i - state.AutoCompleteIndex) * state.LineHeight;

				if (i == state.AutoCompleteSelection) GUIDrawUtils.FillRoundRect(new Rect(bounds.x, y, bounds.width, state.LineHeight), state.Theme.AutoCompleteSelectedBackgroundColor, 0);

				GUI.color = Color.black;
				GUI.Label(new Rect(bounds.x, y, (longestFieldName + 3) * state.CharWidth, state.LineHeight), fieldName);
				GUI.color = Color.gray;
				GUI.Label(new Rect(bounds.x + (longestFieldName + 3) * state.CharWidth, y, longestTypeName * state.CharWidth, state.LineHeight), fieldType);
			}

			GUI.EndClip();
		}

		#endregion

		GUI.color = oldColor;
	}

	private static string HandleKeyDownEvent(string input, CodeAreaState state, out bool canShowAutoComplete) {
		// ok, this is so stupid, on Windows we get a correct KeyCode with a null key char and a None KeyCode with the correct char for each input character
		// so in a press of 'a' would result in the events [KeyCode.A, char \0] and [KeyCode.None, char 'a']
		// char \0 is very problematic because inserting it into a string will end that string at that position
		canShowAutoComplete = false;
		switch (Event.current.keyCode) {
			case KeyCode.Backspace:
				if (state.SelectionStart == state.SelectionEnd) {
					if (state.SelectionStart > 0) {
						input = input.Remove(state.SelectionStart - 1, 1);
						state.SetCursor(state.SelectionStart - 1);
					}
				}
				else {
					input = input.Remove(state.SelectionStart, state.SelectionEnd - state.SelectionStart);
					state.SetCursor(state.SelectionStart);
				}

				Event.current.Use();
				break;
			case KeyCode.Delete:
				if (state.SelectionStart == state.SelectionEnd) {
					if (state.SelectionStart < input.Length) input = input.Remove(state.SelectionStart, 1);
				}
				else {
					input = input.Remove(state.SelectionStart, state.SelectionEnd - state.SelectionStart);
					state.SetCursor(state.SelectionStart);
				}

				Event.current.Use();
				break;
			case KeyCode.Return: // enter
			case KeyCode.Tab:
				if (state.ShowAutoComplete) {
					input = ReplaceSelection(input, state, state.AutoCompleteTextInserts[state.AutoCompleteSelection]);
					state.HideAutoComplete();
				}
				else {
					input = ReplaceSelection(input, state, Event.current.keyCode == KeyCode.Return ? "\n" : "\t");
				}

				Event.current.Use();
				break;
			case KeyCode.Escape:
				if (state.ShowAutoComplete) {
					state.HideAutoComplete();
					Event.current.Use();
				}

				break;
			case KeyCode.LeftArrow:
			case KeyCode.RightArrow:
				int direction = Event.current.keyCode == KeyCode.LeftArrow ? -1 : 1;
				if (Event.current.shift) state.SelectionEndInternal += direction;
				else state.SetCursor(state.SelectionEndInternal + direction);
				state.ClampSelection(input.Length);
				Event.current.Use();
				break;
			case KeyCode.UpArrow:
			case KeyCode.DownArrow:
				bool up = Event.current.keyCode == KeyCode.UpArrow;
				if (state.ShowAutoComplete) {
					state.AutoCompleteSelection = Math.Clamp(state.AutoCompleteSelection + (up ? -1 : 1), 0, state.AutoCompleteOptions.Count - 1);
					if (state.AutoCompleteSelection > Math.Min(state.AutoCompleteIndex + MaxAutoCompleteOptions - 1, state.AutoCompleteOptions.Count - 1)) state.AutoCompleteIndex++;
					else if (state.AutoCompleteSelection < state.AutoCompleteIndex) state.AutoCompleteIndex--;
				}
				else {
					int index = 0;
					for (int i = 0; i < state.Lines.Length; i++) {
						int lineLength = state.LineLengths[i];
						if (index + lineLength >= state.SelectionEndInternal) {
							int lineIndex = state.SelectionEndInternal - index;
							int targetLine = Math.Clamp(up ? i - 1 : i + 1, 0, state.Lines.Length - 1);
							if (targetLine == i) break;

							int targetLineIndex = Math.Clamp(lineIndex, 0, state.LineLengths[targetLine]);
							int targetLineStartIndex = up ? index - (state.LineLengths[targetLine] + 1) : index + lineLength + 1; // +1 for the newline character
							state.SetCursor(targetLineStartIndex + targetLineIndex);
							state.ClampSelection(input.Length); // just in case, should not be necessary
							break;
						}

						index += lineLength + 1;
					}
				}

				Event.current.Use();
				break;
			default:
				char c = Event.current.character;
				if (c >= 32 && c <= 126) // any visible ascii character is in this range
					switch (c) {
						case 'v':
						case 'V':
							input = ReplaceSelection(input, state, Event.current.control ? GUIUtility.systemCopyBuffer : Event.current.character.ToString());
							Event.current.Use();
							break;
						case 'c':
						case 'C':
							if (Event.current.control) GUIUtility.systemCopyBuffer = input.Substring(state.SelectionStart, state.SelectionEnd - state.SelectionStart);
							else input = ReplaceSelection(input, state, Event.current.character.ToString());
							Event.current.Use();
							break;
						case 'a':
						case 'A':
							if (Event.current.control) {
								state.SelectionStartInternal = 0;
								state.SelectionEndInternal = input.Length;
							}
							else {
								input = ReplaceSelection(input, state, Event.current.character.ToString());
							}

							Event.current.Use();
							break;
						case ' ':
							if (Event.current.control)
								state.CreateAutoComplete(input); // this will refresh even if the autocomplete is already shown, to allow the user to refresh options if the tool bugs out
							else input = ReplaceSelection(input, state, Event.current.character.ToString());
							Event.current.Use();
							break;
						default:
							input = ReplaceSelection(input, state, Event.current.character.ToString());
							Event.current.Use();
							break;
					}

				canShowAutoComplete = !char.IsWhiteSpace(c);
				break;
		}

		return input;
	}

	private static string ReplaceSelection(string input, CodeAreaState state, string replacement) {
		if (state.SelectionStart != state.SelectionEnd) input = input.Remove(state.SelectionStart, state.SelectionEnd - state.SelectionStart);
		input = input.Insert(state.SelectionStart, replacement);
		state.SetCursor(state.SelectionStart + replacement.Length);
		return input;
	}

	private static Vector2 GetCodeAreaSize(CodeAreaState state, float minWidth) {
		int maxLineLength = state.LineLengths.Prepend(0).Max();
		Vector2 size = new Vector2(maxLineLength * state.CharWidth, state.Lines.Length * state.LineHeight) + Padding * 2;
		if (size.x < minWidth) size.x = minWidth;
		return size;
	}

	private static void StylizedTextField(Vector2 position, string text, Expression expression, CodeAreaState state) {
		GUI.color = state.Theme.GetExpressionColor(expression);
		GUI.Label(new Rect(position, new Vector2(text.Length * state.CharWidth, state.LineHeight)), text, state.ContentStyle);
	}

	// the draw functions using handles are generated by ChatGPT

	private static Font GetConsolasFont(int size) {
		Font font;
		if (!ConsolasFonts.ContainsKey(size) || ConsolasFonts[size] == null) {
			font = Font.CreateDynamicFontFromOSFont("Consolas", size);
			ConsolasFonts[size] = font;
		} else font = ConsolasFonts[size];
		// according to Unity we should request the characters every frame to ensure they stay loaded
		font.RequestCharactersInTexture(EnglishCharacters, size, FontStyle.Normal);
		return font;
	}
}
}