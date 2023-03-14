using AutoUI.Data;
using AutoUI.Inspector;
using AutoUI.Parsing;
using AutoUI.Parsing.Expressions;
using UnityEngine;

namespace AutoUI.Constraints {
public class ShowConstraint : AutoUIConstraint {
	[SerializeField] private CodeInput conditionConstraint;
	[SerializeField] private bool disableAtRuntime; // since this constraint works even if disabled, we need another way of disabling it

	private Expression conditionExpression;


	protected override void Awake() {
		base.Awake();

		ParseResult parseResult = conditionConstraint.Result;
		conditionExpression = parseResult is { Success: true } ? parseResult.Expression : null;
		if (conditionExpression == null) Debug.LogError("Failed to parse condition expression '" + conditionConstraint.Input + "', defaulting to true.", this);
	}

	public override void Render(DataContext context) {
		if (disableAtRuntime) return;
		try {
			object result = conditionExpression?.Evaluate(context) ?? true;
			if (result == null)
				gameObject.SetActive(false);
			else if (result is bool b)
				gameObject.SetActive(b);
			else
				Debug.LogError("Show condition not a boolean: " + result, this);
		}
		catch (EvaluationException e) {
			Debug.LogException(e, this);
		}
	}
}
}