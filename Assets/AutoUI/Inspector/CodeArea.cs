using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AutoUI.CodeParser;
using AutoUI.CodeParser.Expressions;
using AutoUI.Data;
using UnityEditor;
using UnityEngine;

namespace AutoUI.Inspector {
public class CodeArea {
	private static readonly string EnglishCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!\"#$%&'()*+,-./:;<=>?@[\\]^_`{|}~ \t\n\r";

	private static readonly Color BorderColor = new(0, 0, 0, 1);
	private static readonly Color BorderColorHover = new(0.7f, 0.7f, 0.7f, 1);
	private static readonly Color BorderColorSelected = new(0.23f, 0.47f, 0.73f, 1);
	private static readonly Color BackgroundColor = new(0.165f, 0.165f, 0.165f, 1);
	private static readonly Color SelectionColor = new(0.23f, 0.47f, 0.73f, 1);

	private static readonly Vector2 Padding = new(5, 2); // left, top and right, bottom both have this padding

	public static readonly int MaxAutoCompleteOptions = 5;

	// cached assets
	private static Texture2D SquigglyLineTexture;
	private static readonly Dictionary<int, Texture2D> CircleTextures = new(); // <radius, texture> all of them are white
	private static readonly Dictionary<int, Font> ConsolasFonts = new(); // <size, font>

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
		CodeAreaInfo state = (CodeAreaInfo)GUIUtility.GetStateObject(typeof(CodeAreaInfo), controlId);

		CodeSegmentStyle.font = GetConsolasFont(style.fontSize > 0 ? style.fontSize : style.font.fontSize);
		CodeSegmentStyle.fontSize = CodeSegmentStyle.font.fontSize; // just to be sure
		state.ContentStyle = CodeSegmentStyle;
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

	private static void Draw(Rect position, string input, ParseResult parseResult, bool hasFocus, CodeAreaInfo state) {
		Color oldColor = GUI.color;
		FillRoundRect(position, BackgroundColor, 5);
		DrawRoundRect(position, hasFocus ? BorderColorSelected : BorderColor, 2, 5);
		Rect codeAreaBounds = new(Vector2.zero, GetCodeAreaSize(state, position.width));
		state.scrollPosition = GUI.BeginScrollView(position, state.scrollPosition, codeAreaBounds);
		Rect contentBounds = new(codeAreaBounds.position + Padding, codeAreaBounds.size - Padding * 2);
		GUI.BeginGroup(contentBounds);

		EditorGUIUtility.AddCursorRect(new Rect(Vector2.zero, contentBounds.size), MouseCursor.Text);

		#region Selection

		if (hasFocus) {
			Vector2 cursorPosition = state.GetPositionAtTextIndex(state.SelectionEndInternal);
			FillRoundRect(new Rect(cursorPosition, new Vector2(1, state.LineHeight)), Color.white, 1);
			if (state.SelectionStart != state.SelectionEnd) {
				string selection = input.Substring(state.SelectionStart, state.SelectionEnd - state.SelectionStart);
				string[] lines = selection.Split('\n');
				int index = state.SelectionStart;
				GUI.color = SelectionColor;
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
				DrawSquigglyLine(parseResult.Exception.GetStartPosition(i), parseResult.Exception.GetEndPosition(i), state);
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
			FillRoundRect(bounds, Color.white, 0);
			DrawRoundRect(bounds, Color.black, 1, 0);

			int endIndex = Math.Min(state.AutoCompleteIndex + MaxAutoCompleteOptions, state.AutoCompleteOptions.Count);
			for (int i = state.AutoCompleteIndex; i < endIndex; i++) {
				string fieldName = state.AutoCompleteOptions[i];
				string fieldType = state.AutoCompleteTypes[i].Name;
				float y = bounds.y + (i - state.AutoCompleteIndex) * state.LineHeight;

				if (i == state.AutoCompleteSelection) FillRoundRect(new Rect(bounds.x, y, bounds.width, state.LineHeight), SelectionColor, 0);

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

	private static string HandleKeyDownEvent(string input, CodeAreaInfo state, out bool canShowAutoComplete) {
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

	private static string ReplaceSelection(string input, CodeAreaInfo state, string replacement) {
		if (state.SelectionStart != state.SelectionEnd) input = input.Remove(state.SelectionStart, state.SelectionEnd - state.SelectionStart);
		input = input.Insert(state.SelectionStart, replacement);
		state.SetCursor(state.SelectionStart + replacement.Length);
		return input;
	}

	private static Vector2 GetCodeAreaSize(CodeAreaInfo state, float minWidth) {
		int maxLineLength = state.LineLengths.Prepend(0).Max();
		Vector2 size = new Vector2(maxLineLength * state.CharWidth, state.Lines.Length * state.LineHeight) + Padding * 2;
		if (size.x < minWidth) size.x = minWidth;
		return size;
	}

	private static void StylizedTextField(Vector2 position, string text, Expression expression, CodeAreaInfo state) {
		GUI.color = GetExpressionColor(expression);
		GUI.Label(new Rect(position, new Vector2(text.Length * state.CharWidth, state.LineHeight)), text, state.ContentStyle);
	}

	private static void DrawSquigglyLine(int startPos, int endPos, CodeAreaInfo state) {
		GUI.color = Color.white;

		Texture2D squigglyLineTexture = GetSquigglyLineTexture();

		int lineStart = 0;
		for (int i = 0; i < state.Lines.Length; i++) {
			int lineLength = state.LineLengths[i];
			if (startPos < lineStart + lineLength) {
				int end = Math.Min(endPos, lineStart + lineLength);
				Rect position = new((startPos - lineStart) * state.CharWidth, (i + 1) * state.LineHeight - squigglyLineTexture.height, (end - startPos) * state.CharWidth, squigglyLineTexture.height);
				GUI.DrawTextureWithTexCoords(position, squigglyLineTexture, new Rect(0, 0, position.width / squigglyLineTexture.width, 1));
				startPos = end + 1; // +1 for the newline character
				if (endPos == end) break;
			}

			lineStart += lineLength + 1;
		}
	}

	// the draw functions using handles are generated by ChatGPT
	private static void DrawRoundRect(Rect rect, Color color, float width, float radius) {
		// Draw the four sides
		DrawLine(new Vector2(rect.x + radius, rect.y), new Vector2(rect.x + rect.width - radius, rect.y), color, width);
		DrawLine(new Vector2(rect.x + rect.width, rect.y + radius), new Vector2(rect.x + rect.width, rect.y + rect.height - radius), color, width);
		DrawLine(new Vector2(rect.x + rect.width - radius, rect.y + rect.height), new Vector2(rect.x + radius, rect.y + rect.height), color, width);
		DrawLine(new Vector2(rect.x, rect.y + rect.height - radius), new Vector2(rect.x, rect.y + radius), color, width);

		if (radius <= 0) return;

		// Draw the corners
		DrawArc(new Vector2(rect.x + radius, rect.y + radius), radius, 180f, 270f, color, width);
		DrawArc(new Vector2(rect.x + rect.width - radius, rect.y + radius), radius, 270f, 360f, color, width);
		DrawArc(new Vector2(rect.x + rect.width - radius, rect.y + rect.height - radius), radius, 0f, 90f, color, width);
		DrawArc(new Vector2(rect.x + radius, rect.y + rect.height - radius), radius, 90f, 180f, color, width);
	}

	private static void FillRoundRect(Rect rect, Color color, float radius) {
		Color oldColor = GUI.color;
		GUI.color = color;

		if (radius > 0) {
			Texture2D circle = GetCircleTexture((int)radius);

			// Draw the corners by just drawing different parts of the same circle texture (texture coords are from bottom to top, while UI coords are from top to bottom)
			GUI.DrawTextureWithTexCoords(new Rect(rect.x, rect.y, radius, radius), circle, new Rect(0, 0.5f, 0.5f, 0.5f));
			GUI.DrawTextureWithTexCoords(new Rect(rect.xMax - radius, rect.y, radius, radius), circle, new Rect(0.5f, 0.5f, 0.5f, 0.5f));
			GUI.DrawTextureWithTexCoords(new Rect(rect.x, rect.yMax - radius, radius, radius), circle, new Rect(0, 0, 0.5f, 0.5f));
			GUI.DrawTextureWithTexCoords(new Rect(rect.xMax - radius, rect.yMax - radius, radius, radius), circle, new Rect(0.5f, 0, 0.5f, 0.5f));

			// Draw the sides (the -1 and +2 are to fix line gaps, probably caused by floating point inaccuracies)
			GUI.DrawTexture(new Rect(rect.x + radius - 1, rect.y, rect.width + 2 - 2 * radius, radius), EditorGUIUtility.whiteTexture);
			GUI.DrawTexture(new Rect(rect.x + radius - 1, rect.yMax - radius, rect.width + 2 - 2 * radius, radius), EditorGUIUtility.whiteTexture);
			GUI.DrawTexture(new Rect(rect.x, rect.y + radius, radius, rect.height - 2 * radius), EditorGUIUtility.whiteTexture);
			GUI.DrawTexture(new Rect(rect.xMax - radius, rect.y + radius, radius, rect.height - 2 * radius), EditorGUIUtility.whiteTexture);
		}

		// Draw the center
		GUI.DrawTexture(new Rect(rect.x + radius - 1, rect.y + radius, rect.width + 2 - 2 * radius, rect.height - 2 * radius), EditorGUIUtility.whiteTexture);

		GUI.color = oldColor;
	}

	private static Texture2D GetCircleTexture(int radius) {
		if (CircleTextures.ContainsKey(radius) && CircleTextures[radius] != null) return CircleTextures[radius];

		int diameter = 2 * radius;
		Vector2 center = new(radius, radius);
		Texture2D texture = new(diameter, diameter);
		for (int x = 0; x < diameter; x++)
		for (int y = 0; y < diameter; y++) {
			float distance = Vector2.Distance(new Vector2(x, y), center);
			texture.SetPixel(x, y, distance <= radius ? Color.white : Color.clear);
		}

		texture.Apply();
		CircleTextures[radius] = texture;
		return texture;
	}

	private static Texture2D GetSquigglyLineTexture() {
		if (SquigglyLineTexture != null) return SquigglyLineTexture;


		SquigglyLineTexture = new Texture2D(4, 3);
		SquigglyLineTexture.SetPixels(new[] {
			Color.clear, Color.red, Color.clear, Color.clear,
			Color.red, new(0.85882352941f, 0, 0, 0.25f), Color.red, Color.clear,
			Color.clear, Color.clear, Color.clear, Color.red
		});
		SquigglyLineTexture.Apply(false);
		SquigglyLineTexture.wrapMode = TextureWrapMode.Repeat;
		return SquigglyLineTexture;
	}

	private static Font GetConsolasFont(int size) {
		if (ConsolasFonts.ContainsKey(size) && ConsolasFonts[size] != null) return ConsolasFonts[size];

		Font font = Font.CreateDynamicFontFromOSFont("Consolas", size);
		font.RequestCharactersInTexture(EnglishCharacters, size, FontStyle.Normal);
		ConsolasFonts[size] = font;
		return font;
	}

	private static void DrawLine(Vector2 start, Vector2 end, Color color, float width) {
		Handles.BeginGUI();
		Handles.color = color;
		Handles.DrawAAPolyLine(width, start, end);
		Handles.EndGUI();
	}

	private static void DrawArc(Vector2 center, float radius, float startAngle, float endAngle, Color color, float width) {
		Handles.BeginGUI();
		Handles.color = color;
		Vector3 from = new(Mathf.Cos(Mathf.Deg2Rad * startAngle), Mathf.Sin(Mathf.Deg2Rad * startAngle), 0f);
		Vector3 to = new(Mathf.Cos(Mathf.Deg2Rad * endAngle), Mathf.Sin(Mathf.Deg2Rad * endAngle), 0f);
		Handles.DrawWireArc(center, Vector3.forward, from, endAngle - startAngle, radius);
		Handles.EndGUI();
	}

	private static Color GetExpressionColor(Expression expression) {
		if (expression == null) return Color.white;
		switch (expression) {
			case VariableExpression variableExpression:
				bool? isValid = variableExpression.IsValid();
				if (isValid == null) return Color.white;
				return isValid.Value ? Color.green : Color.red;
			case StringExpression stringExpression:
				return Color.blue;
			default:
				return Color.white;
		}
	}
}

public class CodeAreaInfo : IComparer<Rect> {
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

	public int Compare(Rect a, Rect b) {
		return a.y == b.y ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y);
	}


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
		Expression expression = parseResult.ExpressionsAtPositions.ContainsKey(index) ? parseResult.ExpressionsAtPositions[index] : null;
		if (expression != null)
			for (int i = index - 1; i >= 0; i--)
				if (parseResult.ExpressionsAtPositions[i] != expression) {
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