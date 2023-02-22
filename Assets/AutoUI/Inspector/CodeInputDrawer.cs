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
    private float lastLabelHeight;
    private Vector2 lastPreferredSize;
    
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        lastLabelHeight = GUI.skin.label.CalcSize(label).y;
        GUI.Label(new Rect(position.position, new Vector2(position.width, lastLabelHeight)), label);
        position.y += lastLabelHeight;
        position.height -= lastLabelHeight;

        // parse current input
        SerializedProperty sourceProperty = property.FindPropertyRelative("Input");
        sourceProperty.stringValue = CodeArea.ScrolledArea(position, sourceProperty.stringValue, parseResult, GUI.skin.label, out lastPreferredSize);

        string source = sourceProperty.stringValue;
        if (parseResult == null || source != parseResult.Source) parseResult = CodeInput.GetParseResult(source) ?? parseResult; // will return null if that source has not been parsed yet, in which case we will stick with the old parse result until the new one is ready

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return lastPreferredSize.y + lastLabelHeight;
    }
}