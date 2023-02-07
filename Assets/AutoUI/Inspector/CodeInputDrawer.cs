using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

// Copied from https://forum.unity.com/threads/textarea-font-color.41325/, adjusted to be a PropertyDrawer
[CustomPropertyDrawer(typeof(CodeInput))]
public class CodeInputDrawer : PropertyDrawer
{
    
    private ParseResult parseResult;
    
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        
        // parse current input
        SerializedProperty sourceProperty = property.FindPropertyRelative("Input");
        sourceProperty.stringValue = DrawCodeInput(sourceProperty.stringValue);

        string source = sourceProperty.stringValue;
        if (parseResult == null || source != parseResult.Source) parseResult = CodeInput.GetParseResult(source) ?? parseResult; // will return null if that source has not been parsed yet, in which case we will stick with the old parse result until the new one is ready
        
        EditorGUI.EndProperty();
    }

    private string DrawCodeInput(string input)
    {
        // backup color
        Color backupColor = GUI.color;
        Color backupContentColor = GUI.contentColor;
        Color backupBackgroundColor = GUI.backgroundColor;
       
        // add textarea with transparent text
        GUI.contentColor = new Color(1f, 1f, 1f, 0f);
        GUIStyle style = new GUIStyle(GUI.skin.textArea);
        Rect bounds = new Rect(10, 20, Screen.width - 10, Screen.height - 20);
        input = GUI.TextArea(bounds, input);
       
        // get the text editor of the textarea to control selection
        int controlID = GUIUtility.GetControlID(bounds.GetHashCode(), FocusType.Keyboard); 
        TextEditor editor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), controlID -1);
       
        // set background of all textfield transparent
        GUI.backgroundColor = new Color(1f, 1f, 1f, 0f);   
       
        // backup selection to remake it after process
        int backupCursorIndex = editor.cursorIndex;
        int backupSelectIndex = editor.selectIndex;
       
        // get last position in text
        editor.MoveTextEnd();
        int endpos = editor.cursorIndex;
       
        // draw textfield with color on top of text area
        editor.MoveTextStart();
        while (editor.cursorIndex != endpos)
        {
   
            // set word color
            GUI.contentColor = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
           
            // draw each word with a random color
            Vector2 pixelselpos = style.GetCursorPixelPosition(editor.position, editor.content, editor.selectIndex);
            Vector2 pixelpos = style.GetCursorPixelPosition(editor.position, editor.content, editor.cursorIndex);
            GUI.TextField(new Rect(pixelselpos.x - style.border.left, pixelselpos.y - style.border.top, pixelpos.x, pixelpos.y), wordtext);
            
            editor.MoveToStartOfNextWord();
        }
       
        // Reposition selection
        Vector2 bkpixelselpos = style.GetCursorPixelPosition(editor.position, editor.content, backupSelectIndex);   
        editor.MoveCursorToPosition(bkpixelselpos);
           
        // Remake selection
        Vector2 bkpixelpos = style.GetCursorPixelPosition(editor.position, editor.content, backupCursorIndex); 
        editor.SelectToPosition(bkpixelpos);   
 
        // Reset color
        GUI.color = backupColor;
        GUI.contentColor = backupContentColor;
        GUI.backgroundColor = backupBackgroundColor;

        return input;
    }
}