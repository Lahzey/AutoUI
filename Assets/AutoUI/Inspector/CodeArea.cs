using System;
using System.Collections.Generic;
using System.Numerics;
using UnityEditor;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using Vector4 = UnityEngine.Vector4;

public class CodeArea
{
    private static readonly Color BorderColor = new Color(0, 0, 0, 1);
    private static readonly Color BorderColorHover = new Color(0.7f, 0.7f, 0.7f, 1);
    private static readonly Color BorderColorSelected = new Color(0.23f, 0.47f, 0.73f, 1);
    private static readonly Color BackgroundColor = new Color(0.165f, 0.165f, 0.165f, 1);
    private static readonly Color SelectionColor = new Color(0.23f, 0.47f, 0.73f, 1);
    
    private static Texture2D SquigglyLineTexture = null;
    private static Dictionary<int, Texture2D> CircleTextures = new Dictionary<int, Texture2D>(); // caching circle textures by radius, all of them are white

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
        
        EditorGUIUtility.AddCursorRect(position, MouseCursor.Text);
        
        Action autoCompleteCallback = null;
         
        #region InputHandling

        switch (Event.current.GetTypeForControl(controlId))
        {
            case EventType.KeyDown:
                input = HandleKeyDownEvent(input, state, autoCompleteCallback);
                break;
            case EventType.MouseDown:
                if (!hasFocus && position.Contains(Event.current.mousePosition))
                {
                    GUIUtility.keyboardControl = controlId;
                    hasFocus = true;
                } else if (hasFocus && !position.Contains(Event.current.mousePosition))
                {
                    GUIUtility.keyboardControl = 0;
                    hasFocus = false;
                }

                state.SetCursor(state.GetTextIndexAtPosition(Event.current.mousePosition - position.position));
                Event.current.Use();
                break;
            case EventType.MouseDrag:
                state.SelectionEnd = state.GetTextIndexAtPosition(Event.current.mousePosition - position.position);
                Event.current.Use();
                break;
        }
        
        #endregion
        
        FillRoundRect(position, BackgroundColor, 5);
        DrawRoundRect(position, hasFocus ? BorderColorSelected : BorderColor, 2, 5);
        Rect codeAreaBounds = new Rect(Vector2.zero, GetCodeAreaSize(input, position.width, style));
        state.scrollPosition = GUI.BeginScrollView(new Rect(position.x + 5, position.y + 5, position.width - 10, position.height - 10), state.scrollPosition, codeAreaBounds);
        
        #region Selection
        if (hasFocus)
        {
            if (state.SelectionStart == state.SelectionEnd)
            {
                Vector2 selectionStartPosition = state.GetPositionAtTextIndex(state.SelectionStart);
                DrawLine(selectionStartPosition, selectionStartPosition + new Vector2(0, state.ContentStyle.lineHeight), Color.white, 1);
            }
            else
            {
                string selection = input.Substring(state.SelectionStart, state.SelectionEnd - state.SelectionStart);
                string[] lines = selection.Split('\n');
                int index = state.SelectionStart;
                GUI.color = SelectionColor;
                foreach (string line in lines)
                {
                    Vector2 selectionStartPosition = state.GetPositionAtTextIndex(index);
                    Vector2 selectionEndPosition = state.GetPositionAtTextIndex(index + line.Length);
                    GUI.DrawTexture(new Rect(selectionStartPosition, new Vector2(selectionEndPosition.x - selectionStartPosition.x, state.ContentStyle.lineHeight)), EditorGUIUtility.whiteTexture);
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

        state.ResetContentCache(CodeSegmentStyle);
        Rect elementPosition = new Rect(Vector2.zero, Vector2.zero);
        Expression currentExpression = null;
        int currentExpressionStart = 0;
        for (int i = 0; i < input.Length; i++)
        {
            Expression expression = null;
            parseResult?.ExpressionsAtPositions?.TryGetValue(i, out expression);

            if (expression != currentExpression)
            {
                if (currentExpressionStart < i)
                {
                    // finish up current expression
                    StylizedTextField(input, currentExpressionStart, i, ref elementPosition, currentExpression, state);
                }

                currentExpression = expression;
                currentExpressionStart = i;
            }
        }

        if (currentExpressionStart < input.Length) StylizedTextField(input, currentExpressionStart, input.Length, ref elementPosition, currentExpression, state);

        #endregion

        GUI.EndScrollView();
        return input;
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
                else input = ReplaceSelection(input, state, Event.current.character.ToString());
                break;
            case KeyCode.V:
                input = ReplaceSelection(input, state, Event.current.control ? GUIUtility.systemCopyBuffer : Event.current.character.ToString());
                break;
            case KeyCode.C:
                if (Event.current.control) GUIUtility.systemCopyBuffer = input.Substring(state.SelectionStart, state.SelectionEnd - state.SelectionStart);
                else input = ReplaceSelection(input, state, Event.current.character.ToString());
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

    private static Vector2 GetCodeAreaSize(string input, float minWidth, GUIStyle style)
    {
        Vector4 padding = GetCodeAreaPadding(style);
        Vector2 size = style.CalcSize(new GUIContent(input)) + new Vector2(padding.x + padding.z, padding.y + padding.w);
        if (size.x < minWidth) size.x = minWidth;
        return size;
    }

    private static Vector4 GetCodeAreaPadding(GUIStyle style)
    {
        return new Vector4(style.padding.left, style.padding.top, style.padding.right, style.padding.bottom);
    }

    private static void StylizedTextField(string input, int start, int end, ref Rect position, Expression expression, CodeAreaInfo state)
    {
        GUI.contentColor = GetExpressionColor(expression);

        string[] lines = input.Substring(start, end - start).Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            position.size = state.ContentStyle.CalcSize(new GUIContent(line));
            state.AddContent(position, line, start);
            GUI.Box(position, line, state.ContentStyle);
            start += line.Length + 1; // +1 for the newline character
            position.x += position.width;
            if (i < lines.Length - 1) position.y += position.height;
        }
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

    private int selectionStart;
    private int selectionEnd;
    public int SelectionStart { get => Mathf.Min(selectionStart, selectionEnd); private set => selectionStart = value; }
    public int SelectionEnd { get => Mathf.Max(selectionStart, selectionEnd); set => selectionEnd = value; }

    public GUIStyle ContentStyle { get; private set; } = GUI.skin.label;
    private List<Rect> contentRects = new List<Rect>();
    private Dictionary<Rect, int> contentTextIndices = new Dictionary<Rect, int>();
    private Dictionary<Rect, string> contents = new Dictionary<Rect, string>();
    
    public void SetCursor(int index)
    {
        selectionStart = index;
        selectionEnd = index;
    }

    public void ResetContentCache(GUIStyle contentStyle)
    {
        ContentStyle = contentStyle;
        contentRects.Clear();
        contentTextIndices.Clear();
        contents.Clear();
    }
    
    public void AddContent(Rect rect, string text, int textIndex)
    {
        contentRects.Add(rect);
        contentTextIndices.Add(rect, textIndex);
        contents.Add(rect, text);
        contentRects.Sort(this); // usually this should not be needed as the content is added in order, but sorting a sorted list should not cost much
    }

    public int GetTextIndexAtPosition(Vector2 position)
    {
        position += scrollPosition;
        for (int i = 0; i < contentRects.Count; i++)
        {
            // we can use smaller than checks here because rects to the left or in a previous line have already been checked
            Rect rect = contentRects[i];
            if (position.y < rect.y + rect.height) // correct line
            {
                // if position is to the left of the end of this rect or the next rect is in a different line (indicating that position is to the right of the text area)
                if (position.x < rect.x + rect.width || i == contentRects.Count-1 || contentRects[i + 1].y > rect.y) return GetTextIndexInRect(rect, position);
            }
        }

        return contentRects.Count > 0 ? GetTextIndexInRect(contentRects[^1], position) : 0;
    }

    private int GetTextIndexInRect(Rect rect, Vector2 position)
    {
        return contentTextIndices[rect] + ContentStyle.GetCursorStringIndex(rect, new GUIContent(contents[rect]), position);
    }

    public Vector2 GetPositionAtTextIndex(int textIndex)
    {
        if (contentRects.Count == 0) return Vector2.zero;
        
        foreach (Rect rect in contentRects)
        {
            int rectTextIndex = contentTextIndices[rect];
            string rectText = contents[rect];
            if (textIndex >= rectTextIndex && textIndex < rectTextIndex + rectText.Length)
            {
                return ContentStyle.GetCursorPixelPosition(rect, new GUIContent(rectText), textIndex - rectTextIndex);
            }
        }

        return contentRects[^1].position + new Vector2(contentRects[^1].width, 0); // should not happen
    }

    public int Compare(Rect a, Rect b)
    {
        return a.y == b.y ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y);
    }
}