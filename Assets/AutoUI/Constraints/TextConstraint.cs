using AutoUI.Data;
using AutoUI.Inspector;
using AutoUI.Parsing;
using AutoUI.Parsing.Expressions;
using TMPro;
using UnityEngine;

namespace AutoUI.Constraints {
[RequireComponent(typeof(TextMeshProUGUI))]
public class TextConstraint : AutoUIConstraint {
	[SerializeField] private CodeInput textConstraint;
	private TextMeshProUGUI text;

	private Expression textExpression;

	protected override void Awake() {
		base.Awake();
		text = GetComponent<TextMeshProUGUI>();

		ParseResult parseResult = textConstraint.Result;
		textExpression = parseResult is { Success: true } ? parseResult.Expression : null;
		if (textExpression == null) Debug.LogError("Failed to parse text expression '" + textConstraint.Input + "', defaulting to empty string.", this);
	}

	public override void Render(DataContext context) {
		try {
			text.text = (textExpression?.Evaluate(context) ?? "") + ""; // using plus "" to convert to string because im lazy
		}
		catch (EvaluationException e) {
			Debug.LogException(e, this);
		}
	}
}
}