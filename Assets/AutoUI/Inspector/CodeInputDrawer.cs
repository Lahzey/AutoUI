using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

// Copied from https://forum.unity.com/threads/textarea-font-color.41325/, adjusted to be a PropertyDrawer
[CustomPropertyDrawer(typeof(CodeInput))]
public class CodeInputDrawer : PropertyDrawer
{
    private static readonly float MAX_INPUT_HEIGHT = GUI.skin.textArea.lineHeight * 5;
    private static readonly float BOTTOM_INPUT_PADDING = 4;
    
    private ParseResult parseResult;
    
    private GUIContent textContent;
    private Vector2 scrollPosition = Vector2.zero;
    private float lastWidth = 0;
    private float lastLabelHeight;

    private GUIStyle codeAreaStyle = new GUIStyle(GUI.skin.textArea);
    private GUIStyle codeSegmentStyle = new GUIStyle(GUI.skin.box);
    private Texture2D squigglyLineTexture;
    
    public CodeInputDrawer()
    {
        codeAreaStyle.wordWrap = false;
        
        codeSegmentStyle.normal.textColor = Color.white;
        codeSegmentStyle.normal.background = Texture2D.whiteTexture;
        codeSegmentStyle.alignment = TextAnchor.UpperLeft;
        codeSegmentStyle.padding = new RectOffset(0, 0, 0, 0);
        codeSegmentStyle.margin = new RectOffset(0, 0, 0, 0);
        
        squigglyLineTexture = new Texture2D(4, 3);
        squigglyLineTexture.SetPixels(new Color[]
        {
            Color.clear, Color.red, Color.clear, Color.clear,
            Color.red, new Color(0.85882352941f, 0, 0, 0.25f), Color.red, Color.clear,
            Color.clear, Color.clear, Color.clear, Color.red
        });
        squigglyLineTexture.Apply(false);
        squigglyLineTexture.wrapMode = TextureWrapMode.Repeat;
    }
    
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        lastLabelHeight = GUI.skin.label.CalcSize(label).y;
        GUI.Label(new Rect(position.position, new Vector2(position.width, lastLabelHeight)), label);
        position.y += lastLabelHeight;

        // parse current input
        SerializedProperty sourceProperty = property.FindPropertyRelative("Input");
        // sourceProperty.stringValue = DrawCodeInput(sourceProperty.stringValue, position);
        sourceProperty.stringValue = CodeArea.ScrolledArea(position, sourceProperty.stringValue, parseResult, GUI.skin.label);

        string source = sourceProperty.stringValue;
        if (parseResult == null || source != parseResult.Source) parseResult = CodeInput.GetParseResult(source) ?? parseResult; // will return null if that source has not been parsed yet, in which case we will stick with the old parse result until the new one is ready

        EditorGUI.EndProperty();
    }

    private string DrawCodeInput(string input, Rect position)
    {
        // backup color
        Color backupColor = GUI.color;
        Color backupContentColor = GUI.contentColor;
        Color backupBackgroundColor = GUI.backgroundColor;
        
        // add white textarea with transparent text
        GUI.backgroundColor = Color.white;
        GUI.contentColor = Color.clear;
        textContent = new GUIContent(input);
        Vector2 contentSize = codeAreaStyle.CalcSize(textContent);
        contentSize.y += BOTTOM_INPUT_PADDING;
        contentSize.x = Mathf.Max(contentSize.x, position.width - (contentSize.y > position.height ? GUI.skin.verticalScrollbar.fixedWidth : 0));
        Rect bounds = new Rect(Vector2.zero, contentSize);
        scrollPosition = GUI.BeginScrollView(position, scrollPosition, bounds);
        input = GUI.TextArea(bounds, input, codeAreaStyle);
        textContent = new GUIContent(input);
        lastWidth = position.width;

        // set background of all textfield transparent
        GUI.backgroundColor = Color.clear;
        
        // draw squiggly line under all errors
        if (parseResult != null && parseResult.Exception != null)
        {
            for (int i = 0; i < parseResult.Exception.MessageCount(); i++)
            {
                Debug.Log(parseResult.Exception.GetMessage(i));
                DrawSquigglyLine(input, bounds, parseResult.Exception.GetStartPosition(i), parseResult.Exception.GetEndPosition(i));
            }
        } 
        
       
        // draw textfield with color on top of text area
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
                    StylizedTextField(input, bounds, currentExpressionStart, i, currentExpression);
                }
        
                currentExpression = expression;
                currentExpressionStart = i;
            }
        }
        if (currentExpressionStart < input.Length) StylizedTextField(input, bounds, currentExpressionStart, input.Length, currentExpression);
        
        GUI.EndScrollView();

        // Reset color
        GUI.color = backupColor;
        GUI.contentColor = backupContentColor;
        GUI.backgroundColor = backupBackgroundColor;

        return input;
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (textContent == null) return 100f; // doesn't matter what will be returned here, will be recalculated on next draw
        
        Vector2 contentSize = codeAreaStyle.CalcSize(textContent);
        contentSize.y += BOTTOM_INPUT_PADDING;
        float height = Mathf.Min(contentSize.y, MAX_INPUT_HEIGHT);
        return (contentSize.x > lastWidth ? height + GUI.skin.horizontalScrollbar.fixedHeight : height) + lastLabelHeight;
    }

    private void StylizedTextField(string text, Rect bounds, int startPos, int endPos, Expression expression)
    {
        startPos = Mathf.Min(startPos, text.Length - 1);
        endPos = Mathf.Min(endPos, text.Length);
        GUI.contentColor = GetExpressionColor(expression);

        string substring = text.Substring(startPos, endPos - startPos);
        string[] lines = substring.Split('\n');
        int pos = startPos;
        foreach (string line in lines)
        {
            Vector2 pixelStartPos = GUI.skin.textArea.GetCursorPixelPosition(bounds, textContent, pos);
            Vector2 pixelEndPos = GUI.skin.textArea.GetCursorPixelPosition(bounds, textContent, pos + line.Length);
            GUI.Box(new Rect(pixelStartPos.x, pixelStartPos.y, pixelEndPos.x - pixelStartPos.x, GUI.skin.textArea.lineHeight), line, codeSegmentStyle);
            pos += line.Length + 1; // +1 for the newline
        }
    }
    
    private void DrawSquigglyLine(string text, Rect bounds, int startPos, int endPos)
    {
        startPos = Mathf.Min(startPos, text.Length - 1);
        endPos = Mathf.Min(endPos, text.Length);
        string substring = text.Substring(startPos, endPos - startPos);
        string[] lines = substring.Split('\n');
        int pos = startPos;
        foreach (string line in lines)
        {
            Vector2 pixelStartPos = GUI.skin.textArea.GetCursorPixelPosition(bounds, textContent, pos);
            Vector2 pixelEndPos = GUI.skin.textArea.GetCursorPixelPosition(bounds, textContent, pos + line.Length);
            Rect position = new Rect(pixelStartPos.x, pixelStartPos.y + GUI.skin.textArea.lineHeight, pixelEndPos.x - pixelStartPos.x, 3);
            GUI.DrawTextureWithTexCoords(position, squigglyLineTexture, new Rect(0, 0, position.width / squigglyLineTexture.width, 1));
            pos += line.Length + 1; // +1 for the newline
        }
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