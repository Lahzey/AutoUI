using AutoUI.Data;
using AutoUI.Inspector;
using AutoUI.Parsing;
using AutoUI.Parsing.Expressions;
using UnityEngine;

namespace AutoUI.Constraints {
[RequireComponent(typeof(RectTransform))]
public class RectTransformConstraint : AutoUIConstraint {
	// anchor constraints
	[SerializeField] private CodeInput minXConstraint;
	[SerializeField] private CodeInput maxXConstraint;
	[SerializeField] private CodeInput minYConstraint;
	[SerializeField] private CodeInput maxYConstraint;

	private Expression[] expressions;
	private RectTransform rectTransform;

	private CodeInput[] constraints => new[] { minXConstraint, maxXConstraint, minYConstraint, maxYConstraint };

	protected override void Awake() {
		base.Awake();
		rectTransform = GetComponent<RectTransform>();

		CodeInput[] constraints = this.constraints;
		expressions = new Expression[constraints.Length];
		for (int i = 0; i < constraints.Length; i++) {
			CodeInput constraint = constraints[i];
			if (constraint.Input.Length == 0) continue;
			ParseResult parseResult = constraint.Result;
			if (parseResult is not { Success: true }) Debug.LogError("Failed to parse RectTransformConstraint expression '" + constraint.Input + "'.", this);
			else expressions[i] = parseResult.Expression;
		}
	}

	// Update is called once per frame
	public override void Render(DataContext context) {
		// Values here must be in the same order as defined by the constraints property. Sorry, I know its not clean :(
		float[] values = new float[4];
		values[0] = rectTransform.anchorMin.x;
		values[1] = rectTransform.anchorMax.x;
		values[2] = rectTransform.anchorMin.y;
		values[3] = rectTransform.anchorMax.y;


		for (int i = 0; i < expressions.Length; i++) {
			Expression expression = expressions[i];
			if (expression != null) {
				object value = expression.Evaluate(context);
				if (value is short or int or long or float or double) values[i] = (float)value;
				else Debug.LogError("RectTransformConstraint expression '" + constraints[i].Input + "' does not evaluate to a number. Result: " + value);
			}
		}

		rectTransform.anchorMin = new Vector2(values[0], values[2]);
		rectTransform.anchorMax = new Vector2(values[1], values[3]);
	}
}
}