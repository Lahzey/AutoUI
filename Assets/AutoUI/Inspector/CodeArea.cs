using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using UnityEditor;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using Vector4 = UnityEngine.Vector4;

public class CodeArea
{
    private static readonly string EnglishCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!\"#$%&'()*+,-./:;<=>?@[\\]^_`{|}~ \t\n\r";
    
    private static readonly Color BorderColor = new Color(0, 0, 0, 1);
    private static readonly Color BorderColorHover = new Color(0.7f, 0.7f, 0.7f, 1);
    private static readonly Color BorderColorSelected = new Color(0.23f, 0.47f, 0.73f, 1);
    private static readonly Color BackgroundColor = new Color(0.165f, 0.165f, 0.165f, 1);
    private static readonly Color SelectionColor = new Color(0.23f, 0.47f, 0.73f, 1);
    
    private static readonly Vector2 Padding = new Vector2(5, 2); // left, top and right, bottom both have this padding
    
    // cached assets
    private static Texture2D SquigglyLineTexture = null;
    private static Dictionary<int, Texture2D> CircleTextures = new Dictionary<int, Texture2D>(); // <radius, texture> all of them are white
    private static Dictionary<int, Font> ConsolasFonts = new Dictionary<int, Font>(); // <size, font>
    
    private static GUIStyle CodeSegmentStyle;

    static CodeArea()
    {
        CodeSegmentStyle = new GUIStyle(GUI.skin.label);
        CodeSegmentStyle.margin = new RectOffset(0, 0, 0, 0);
        CodeSegmentStyle.border = new RectOffset(0, 0, 0, 0);
        CodeSegmentStyle.padding = new RectOffset(0, 0, 0, 0);
        CodeSegmentStyle.alignment = TextAnchor.UpperLeft;
    }

    public static string ScrolledArea(Rect position, string input, ParseResult parseResult, GUIStyle style)
    {
        int controlId = GUIUtility.GetControlID(FocusType.Keyboard);
        bool hasFocus = GUIUtility.keyboardControl == controlId;
        CodeAreaInfo state = (CodeAreaInfo) GUIUtility.GetStateObject(typeof(CodeAreaInfo), controlId);
        
        CodeSegmentStyle.font = GetConsolasFont(style.fontSize > 0 ? style.fontSize : style.font.fontSize);
        CodeSegmentStyle.fontSize = CodeSegmentStyle.font.fontSize; // just to be sure
        state.ContentStyle = CodeSegmentStyle;
        state.SetLines(input.Split('\n'));

        Action autoCompleteCallback = null; // TODO autocomplete
        Rect inputBounds = new Rect(position.position + Padding, position.size - Padding * 2);
        switch (Event.current.GetTypeForControl(controlId))
        {
            case EventType.KeyDown:
                string newInput = HandleKeyDownEvent(input, state, autoCompleteCallback);
                if (newInput != input) GUI.changed = true;
                input = newInput;
                break;
            case EventType.MouseDown:
                if (!hasFocus && inputBounds.Contains(Event.current.mousePosition))
                {
                    GUIUtility.keyboardControl = controlId;
                } else if (hasFocus && !inputBounds.Contains(Event.current.mousePosition))
                {
                    GUIUtility.keyboardControl = 0;
                }

                state.SetCursor(state.GetTextIndexAtPosition(Event.current.mousePosition - inputBounds.position));
                Event.current.Use();
                break;
            case EventType.MouseDrag:
                state.SelectionEndInternal = state.GetTextIndexAtPosition(Event.current.mousePosition - inputBounds.position);
                Event.current.Use();
                break;
            case EventType.Repaint:
                Draw(position, input, parseResult, hasFocus, state, style);
                break;
        }
        
        return input;
    }

    private static void Draw(Rect position, string input, ParseResult parseResult, bool hasFocus, CodeAreaInfo state, GUIStyle style)
    {
        
        FillRoundRect(position, BackgroundColor, 5);
        DrawRoundRect(position, hasFocus ? BorderColorSelected : BorderColor, 2, 5);
        Rect codeAreaBounds = new Rect(Vector2.zero, GetCodeAreaSize(state, position.width));
        state.scrollPosition = GUI.BeginScrollView(position, state.scrollPosition, codeAreaBounds);
        Rect contentBounds = new Rect(codeAreaBounds.position + Padding, codeAreaBounds.size - Padding * 2);
        GUI.BeginGroup(contentBounds);
        
        EditorGUIUtility.AddCursorRect(new Rect(Vector2.zero, contentBounds.size), MouseCursor.Text);
        
        #region Selection
        if (hasFocus)
        {
            Vector2 cursorPosition = state.GetPositionAtTextIndex(state.SelectionEndInternal);
            DrawLine(cursorPosition, cursorPosition + new Vector2(0, state.LineHeight), Color.white, 1);
            if (state.SelectionStart != state.SelectionEnd)
            {
                string selection = input.Substring(state.SelectionStart, state.SelectionEnd - state.SelectionStart);
                string[] lines = selection.Split('\n');
                int index = state.SelectionStart;
                GUI.color = SelectionColor;
                foreach (string line in lines)
                {
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
        {
            for (int i = 0; i < parseResult.Exception.MessageCount(); i++)
            {
                Debug.Log(parseResult.Exception.GetMessage(i));
                DrawSquigglyLine(input, parseResult.Exception.GetStartPosition(i), parseResult.Exception.GetEndPosition(i), CodeSegmentStyle);
            }
        }

        #endregion

        #region CodeElements
        int charCountBeforeLine = 0;
        for (int lineIndex = 0; lineIndex < state.Lines.Length; lineIndex++)
        {
            Expression currentExpression = null;
            int currentExpressionStart = 0;
            int y = lineIndex * state.LineHeight;
            for (int indexInLine = 0; indexInLine < state.LineLengths[lineIndex]; indexInLine++)
            {
                Expression expression = null;
                parseResult?.ExpressionsAtPositions?.TryGetValue(indexInLine, out expression);

                if (expression != currentExpression)
                {
                    if (currentExpressionStart < indexInLine)
                    {
                        // finish up current expression
                        StylizedTextField(new Vector2(currentExpressionStart * state.CharWidth, y), state.Lines[lineIndex].Substring(currentExpressionStart, indexInLine - currentExpressionStart), currentExpression, state);
                    }

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
    }

    private void HandleInput(CodeAreaInfo state, int controlId)
    {
        
    }

    private static string HandleKeyDownEvent(string input, CodeAreaInfo state, Action autoCompleteCallback)
    {
        switch (Event.current.keyCode)
        {
            case KeyCode.Backspace:
                if (state.SelectionStart == state.SelectionEnd)
                {
                    if (state.SelectionStart > 0)
                    {
                        input = input.Remove(state.SelectionStart - 1, 1);
                        state.SetCursor(state.SelectionStart - 1);
                    }
                }
                else
                {
                    input = input.Remove(state.SelectionStart, state.SelectionEnd - state.SelectionStart);
                    state.SetCursor(state.SelectionStart);
                }
                Event.current.Use();
                break;
            case KeyCode.Delete:
                if (state.SelectionStart == state.SelectionEnd)
                {
                    if (state.SelectionStart < input.Length) input = input.Remove(state.SelectionStart, 1);
                }
                else
                {
                    input = input.Remove(state.SelectionStart, state.SelectionEnd - state.SelectionStart);
                    state.SetCursor(state.SelectionStart);
                }
                Event.current.Use();
                break;
            case KeyCode.Return: // enter
            case KeyCode.Tab:
                if (autoCompleteCallback != null) autoCompleteCallback();
                else input = ReplaceSelection(input, state, Event.current.keyCode == KeyCode.Return ? "\n" : "\t");
                Event.current.Use();
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
                if (1 == 0) // TODO: if currently showing autocomplete
                {
                    
                }
                else
                {
                    bool up = Event.current.keyCode == KeyCode.UpArrow;
                    string[] lines = input.Split('\n');
                    int index = 0;
                    for (int i = 0; i < lines.Length; i++)
                    {
                        string line = lines[i];
                        if (index + line.Length >= state.SelectionEndInternal)
                        {
                            int lineIndex = state.SelectionEndInternal - index;
                            int targetLine = Math.Clamp(up ? lineIndex - 1 : lineIndex + 1, 0, lines.Length - 1);
                            if (targetLine == i) break;

                            int targetLineIndex = Math.Clamp(lineIndex, 0, lines[targetLine].Length - 1);
                            int targetLineStartIndex = up ? index - lines[targetLine].Length : index + line.Length;
                            state.SetCursor(targetLineStartIndex + targetLineIndex);
                            state.ClampSelection(input.Length); // just in case, should not be necessary
                            break;
                        }
                        index += line.Length + 1;
                    }
                }
                Event.current.Use();
                break;
            case KeyCode.V:
                input = ReplaceSelection(input, state, Event.current.control ? GUIUtility.systemCopyBuffer : Event.current.character.ToString());
                Event.current.Use();
                break;
            case KeyCode.C:
                if (Event.current.control) GUIUtility.systemCopyBuffer = input.Substring(state.SelectionStart, state.SelectionEnd - state.SelectionStart);
                else input = ReplaceSelection(input, state, Event.current.character.ToString());
                Event.current.Use();
                break;
            default:
                char c = Event.current.character;
                if (c >= 32 && c <= 126) // any visible ascii character is in this range
                {
                    input = ReplaceSelection(input, state, Event.current.character.ToString());
                    Event.current.Use();
                }
                break;
        }

        return input;
    }
    
    private static string ReplaceSelection(string input, CodeAreaInfo state, string replacement)
    {
        if (state.SelectionStart != state.SelectionEnd) input = input.Remove(state.SelectionStart, state.SelectionEnd - state.SelectionStart);
        input = input.Insert(state.SelectionStart, replacement);
        state.SetCursor(state.SelectionStart + replacement.Length);
        return input;
    }

    private static Vector2 GetCodeAreaSize(CodeAreaInfo state, float minWidth)
    {
        int maxLineLength = state.LineLengths.Prepend(0).Max();
        Vector2 size = new Vector2(maxLineLength * state.CharWidth, state.Lines.Length * state.LineHeight) + Padding * 2;
        if (size.x < minWidth) size.x = minWidth;
        return size;
    }

    private static void StylizedTextField(Vector2 position, string text, Expression expression, CodeAreaInfo state)
    {
        GUI.contentColor = GetExpressionColor(expression);
        GUI.Box(new Rect(position, new Vector2(text.Length * state.CharWidth, state.LineHeight)), text, state.ContentStyle);
    }

    private static void DrawSquigglyLine(string text, int startPos, int endPos, GUIStyle style)
    {
        startPos = Mathf.Min(startPos, text.Length - 1);
        endPos = Mathf.Min(endPos, text.Length);

        int linesAboveCount = 0;
        int thisLineStartPos = 0;
        for (int i = 0; i < startPos; i++)
        {
            if (text[i] == '\n')
            {
                linesAboveCount++;
                thisLineStartPos = i + 1;
            }
        }

        float xPos = style.CalcSize(new GUIContent(text.Substring(thisLineStartPos, startPos - thisLineStartPos))).x;
        Rect position = new Rect(xPos, style.lineHeight, 0, 0);
        string[] lines = text.Substring(startPos, endPos - startPos).Split('\n');
        int pos = startPos;
        Texture2D squigglyLineTexture = GetSquigglyLineTexture();
        foreach (string line in lines)
        {
            position.size = style.CalcSize(new GUIContent(text.Substring(startPos, endPos - startPos)));
            GUI.DrawTextureWithTexCoords(position, squigglyLineTexture, new Rect(0, 0, position.width / squigglyLineTexture.width, 1));
            position.x += position.width;
            position.y += position.height;
        }
    }
    
    // the draw functions using handles are generated by ChatGPT
    private static void DrawRoundRect(Rect rect, Color color, float width, float radius)
    {
        // Draw the four sides
        DrawLine(new Vector2(rect.x + radius, rect.y), new Vector2(rect.x + rect.width - radius, rect.y), color, width);
        DrawLine(new Vector2(rect.x + rect.width, rect.y + radius), new Vector2(rect.x + rect.width, rect.y + rect.height - radius), color, width);
        DrawLine(new Vector2(rect.x + rect.width - radius, rect.y + rect.height), new Vector2(rect.x + radius, rect.y + rect.height), color, width);
        DrawLine(new Vector2(rect.x, rect.y + rect.height - radius), new Vector2(rect.x, rect.y + radius), color, width);

        // Draw the corners
        DrawArc(new Vector2(rect.x + radius, rect.y + radius), radius, 180f, 270f, color, width);
        DrawArc(new Vector2(rect.x + rect.width - radius, rect.y + radius), radius, 270f, 360f, color, width);
        DrawArc(new Vector2(rect.x + rect.width - radius, rect.y + rect.height - radius), radius, 0f, 90f, color, width);
        DrawArc(new Vector2(rect.x + radius, rect.y + rect.height - radius), radius, 90f, 180f, color, width);
    }
    
    private static void FillRoundRect(Rect rect, Color color, float radius)
    {
        GUI.color = color;

        Texture2D circle = GetCircleTexture((int) radius);

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
        
        // Draw the center
        GUI.DrawTexture(new Rect(rect.x + radius - 1, rect.y + radius, rect.width + 2 - 2 * radius, rect.height - 2 * radius), EditorGUIUtility.whiteTexture);
    }

    private static Texture2D GetCircleTexture(int radius)
    {
        if (CircleTextures.ContainsKey(radius) && CircleTextures[radius] != null) return CircleTextures[radius];
        
        int diameter = 2 * radius;
        Vector2 center = new Vector2(radius, radius);
        Texture2D texture = new Texture2D(diameter, diameter);
        for (int x = 0; x < diameter; x++)
        {
            for (int y = 0; y < diameter; y++)
            {
                
                float distance = Vector2.Distance(new Vector2(x, y), center);
                texture.SetPixel(x, y, distance <= radius ? Color.white : Color.clear);
            }
        }
        texture.Apply();
        CircleTextures.Add(radius, texture);
        return texture;
    }

    private static Texture2D GetSquigglyLineTexture()
    {
        if (SquigglyLineTexture != null) return SquigglyLineTexture;
        
        
        SquigglyLineTexture = new Texture2D(4, 3);
        SquigglyLineTexture.SetPixels(new Color[]
        {
            Color.clear, Color.red, Color.clear, Color.clear,
            Color.red, new Color(0.85882352941f, 0, 0, 0.25f), Color.red, Color.clear,
            Color.clear, Color.clear, Color.clear, Color.red
        });
        SquigglyLineTexture.Apply(false);
        SquigglyLineTexture.wrapMode = TextureWrapMode.Repeat;
        return SquigglyLineTexture;
    }

    private static Font GetConsolasFont(int size)
    {
        if (ConsolasFonts.ContainsKey(size) && ConsolasFonts[size] != null) return ConsolasFonts[size];
        
        Font font = Font.CreateDynamicFontFromOSFont("Consolas", size);
        font.RequestCharactersInTexture(EnglishCharacters, size, FontStyle.Normal);
        ConsolasFonts.Add(size, font);
        return font;
    }

    private static void DrawLine(Vector2 start, Vector2 end, Color color, float width)
    {
        Handles.BeginGUI();
        Handles.color = color;
        Handles.DrawAAPolyLine(width, start, end);
        Handles.EndGUI();
    }

    private static void DrawArc(Vector2 center, float radius, float startAngle, float endAngle, Color color, float width)
    {
        Handles.BeginGUI();
        Handles.color = color;
        Vector3 from = new Vector3(Mathf.Cos(Mathf.Deg2Rad * startAngle), Mathf.Sin(Mathf.Deg2Rad * startAngle), 0f);
        Vector3 to = new Vector3(Mathf.Cos(Mathf.Deg2Rad * endAngle), Mathf.Sin(Mathf.Deg2Rad * endAngle), 0f);
        Handles.DrawWireArc(center, Vector3.forward, from, endAngle - startAngle, radius);
        Handles.EndGUI();
    }

    private static Color GetExpressionColor(Expression expression)
    {
        if (expression == null) return Color.white;
        switch (expression)
        {
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

public class CodeAreaInfo : IComparer<Rect>
{
    public Vector2 scrollPosition;

    public int SelectionStartInternal;
    public int SelectionEndInternal;
    public int SelectionStart => Mathf.Min(SelectionStartInternal, SelectionEndInternal);
    public int SelectionEnd => Mathf.Max(SelectionStartInternal, SelectionEndInternal);

    public GUIStyle ContentStyle;
    public int LineHeight => ContentStyle.font.lineHeight;
    public int CharWidth => ContentStyle.font.characterInfo[0].advance; // doesn't matter which character, all consolas chars are the same width
    public readonly List<int> LineLengths = new List<int>();
    public string[] Lines { get; private set; }


    public void SetCursor(int index)
    {
        SelectionStartInternal = index;
        SelectionEndInternal = index;
    }

    public void ClampSelection(int inputLength)
    {
        SelectionStartInternal = Math.Clamp(SelectionStartInternal, 0, inputLength);
        SelectionEndInternal = Math.Clamp(SelectionEndInternal, 0, inputLength);
    }

    public void SetLines(string[] lines)
    {
        Lines = lines;
        LineLengths.Clear();
        foreach (string line in lines)
        {
            LineLengths.Add(line.Length);
        }
    }

    public int GetTextIndexAtPosition(Vector2 position)
    {
        int lineIndex = Math.Clamp(Mathf.FloorToInt(position.y / LineHeight), 0, LineLengths.Count - 1);
        int lineLength = LineLengths[lineIndex];
        int charIndex = Math.Clamp(Mathf.RoundToInt(position.x / CharWidth), 0, lineLength);
        int textIndex = 0;
        for (int i = 0; i < lineIndex; i++)
        {
            textIndex += LineLengths[i] + 1; // +1 for newline
        }
        textIndex += charIndex;
        return textIndex;
    }

    public Vector2 GetPositionAtTextIndex(int textIndex)
    {
        int index = 0;
        for (int i = 0; i < LineLengths.Count; i++)
        {
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

    public int Compare(Rect a, Rect b)
    {
        return a.y == b.y ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y);
    }
}