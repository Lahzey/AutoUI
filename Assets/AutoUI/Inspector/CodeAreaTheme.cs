using AutoUI.Parsing.Expressions;
using UnityEngine;

namespace AutoUI.Inspector {
public class CodeAreaTheme {
	// Colors used by Unity for the Inspector, according to ChatGPT (seems to be correct, how the hell does it know though?)
	private static readonly Color BackgroundUnityDark = new Color(0.172549f, 0.172549f, 0.172549f);
	private static readonly Color BackgroundUnityLight = new Color(0.952941f, 0.952941f, 0.952941f);
	private static readonly Color ForegroundUnityDark = new Color(0.866667f, 0.866667f, 0.866667f);
	private static readonly Color ForegroundUnityLight = new Color(0.2f, 0.2f, 0.2f);
	private static readonly Color NormalBorderUnityDark = new Color(0.235294f, 0.235294f, 0.235294f);
	private static readonly Color NormalBorderUnityLight = new Color(0.760784f, 0.760784f, 0.760784f);
	private static readonly Color HoverBorderUnityDark = new Color(0.360784f, 0.360784f, 0.360784f);
	private static readonly Color HoverBorderUnityLight = new Color(0.643137f, 0.643137f, 0.643137f);
	private static readonly Color SelectedBorderUnityDark = new Color(0.0f, 0.478431f, 0.8f);
	private static readonly Color SelectedBorderUnityLight = new Color(0.0f, 0.478431f, 0.8f);
	
	private static readonly Color SelectionTextBackground = new(0.23f, 0.47f, 0.73f, 1);

	private static readonly Color RiderAutoCompleteNormalBackground = new Color(0.9686f, 0.9686f, 0.9686f);
	private static readonly Color RiderAutoCompleteSelectedBackground = new Color(0.77255f, 0.8745f, 0.988f);
	private static readonly Color RiderAutoCompleteBorder = new Color(0.67058f, 0.67058f, 0.67058f);

	public static readonly CodeAreaTheme DEFAULT_DARK = new CodeAreaTheme() { // non-Unity colors are taken from IntelliJ's 'Dark' Theme
		BackgroundColor = BackgroundUnityDark,
		NormalBorderColor = NormalBorderUnityDark,
		HoverBorderColor = HoverBorderUnityDark,
		SelectedBorderColor = SelectedBorderUnityDark,
		DefaultTextColor = new Color(0.6627451f, 0.7176471f, 0.7764706f),
		KeywordTextColor = new Color(0.8f, 0.47058824f, 0.19607843f),
		StringTextColor = new Color(0.41568628f, 0.5294118f, 0.34901962f),
		NumberTextColor = new Color(0.40784314f, 0.5921569f, 0.73333335f),
		CommentTextColor = new Color(0.5019608f, 0.5019608f, 0.5019608f),
		TypeTextColor = new Color(1.0f, 0.7764706f, 0.42745098f), // color taken from 'Constructor declaration'
		VariableTextColor = new Color(0.3137255f, 0.47058824f, 0.45490196f), // color taken from 'Type parameter'
		FieldTextColor = new Color(0.59607846f, 0.4627451f, 0.6666667f),
		ErrorTextColor = new Color(0.7372549f, 0.24705882f, 0.23529412f),
		CursorColor = Color.white,
		SelectionTextBackgroundColor = SelectionTextBackground,
		ErrorUnderlineColor = new Color(0.7372549f, 0.24705882f, 0.23529412f),
		WarningUnderlineColor = new Color(0.68235296f, 0.68235296f, 0.5019608f),
		AutoCompleteNormalBackgroundColor = RiderAutoCompleteNormalBackground,
		AutoCompleteSelectedBackgroundColor = RiderAutoCompleteSelectedBackground,
		AutoCompleteBorderColor = RiderAutoCompleteBorder
	};

	public static readonly CodeAreaTheme DEFAULT_LIGHT = new CodeAreaTheme() { // non-Unity colors are taken from IntelliJ's 'IntelliJ Light' Theme
		BackgroundColor = BackgroundUnityLight,
		NormalBorderColor = NormalBorderUnityLight,
		HoverBorderColor = HoverBorderUnityLight,
		SelectedBorderColor = SelectedBorderUnityLight,
		DefaultTextColor = new Color(0.03137255f, 0.03137255f, 0.03137255f),
		KeywordTextColor = new Color(0.0f, 0.2f, 0.7019608f),
		StringTextColor = new Color(0.023529412f, 0.49019608f, 0.09019608f),
		NumberTextColor = new Color(0.09019608f, 0.3137255f, 0.92156863f),
		CommentTextColor = new Color(0.54901963f, 0.54901963f, 0.54901963f),
		TypeTextColor = new Color(0.0f, 0.38431373f, 0.47843137f), // color taken from 'Constructor declaration'
		VariableTextColor = new Color(0.0f, 0.49411765f, 0.5411765f), // color taken from 'Type parameter'
		FieldTextColor = new Color(0.5294118f, 0.0627451f, 0.5803922f),
		ErrorTextColor = new Color(0.9607843f, 0.0f, 0.0f),
		CursorColor = Color.black,
		SelectionTextBackgroundColor = SelectionTextBackground,
		ErrorUnderlineColor = new Color(1.0f, 0.0f, 0.0f),
		WarningUnderlineColor = new Color(0.95686275f, 0.59607846f, 0.0627451f),
		AutoCompleteNormalBackgroundColor = RiderAutoCompleteNormalBackground,
		AutoCompleteSelectedBackgroundColor = RiderAutoCompleteSelectedBackground,
		AutoCompleteBorderColor = RiderAutoCompleteBorder
	};
	
	
	// UI element colors
	public Color BackgroundColor { get; private set; }
	public Color NormalBorderColor { get; private set; }
	public Color HoverBorderColor { get; private set; }
	public Color SelectedBorderColor { get; private set; }
	
	// text colors
	public Color DefaultTextColor { get; private set; }
	public Color KeywordTextColor { get; private set; }
	public Color StringTextColor { get; private set; }
	public Color NumberTextColor { get; private set; }
	public Color CommentTextColor { get; private set; }
	public Color TypeTextColor { get; private set; }
	public Color VariableTextColor { get; private set; }
	public Color FieldTextColor { get; private set; }
	public Color ErrorTextColor { get; private set; }
	public Color CursorColor { get; private set; }
	public Color SelectionTextBackgroundColor { get; private set; }
	
	// underline colors
	public Color ErrorUnderlineColor { get; private set; }
	public Color WarningUnderlineColor { get; private set; }
	
	// autocomplete colors
	public Color AutoCompleteNormalBackgroundColor { get; private set; }
	public Color AutoCompleteSelectedBackgroundColor { get; private set; }
	public Color AutoCompleteBorderColor { get; private set; }
	
	
	

	public Color GetExpressionColor(Expression expression) {
		if (expression == null) return Color.white;
		switch (expression) {
			case VariableExpression variableExpression:
				bool? isValid = variableExpression.IsValid();
				if (isValid == null) return DefaultTextColor;
				return isValid.Value ? variableExpression.contextType == null ? VariableTextColor : FieldTextColor : ErrorTextColor;
			case StringExpression:
				return StringTextColor;
			case NumberExpression:
				return NumberTextColor;
			default:
				return DefaultTextColor;
		}
	}
}
}