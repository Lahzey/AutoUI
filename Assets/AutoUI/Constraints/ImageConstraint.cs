using AutoUI.Data;
using AutoUI.Inspector;
using AutoUI.Parsing;
using AutoUI.Parsing.Expressions;
using UnityEngine;
using UnityEngine.UI;

namespace AutoUI.Constraints {
[RequireComponent(typeof(Image))]
public class ImageConstraint : AutoUIConstraint {
	[SerializeField] private CodeInput imageConstraint;
	private Image imageComponent;

	private Expression imageExpression;

	protected override void Awake() {
		base.Awake();
		imageComponent = GetComponent<Image>();

		ParseResult parseResult = imageConstraint.Result;
		imageExpression = parseResult is { Success: true } ? parseResult.Expression : null;
		if (imageExpression == null) Debug.LogError("Failed to parse image expression '" + imageConstraint.Input + "', defaulting to null.", this);
	}

	public override void Render(DataContext context) {
		try {
			object image = imageExpression?.Evaluate(context) ?? null;
			switch (image) {
				case null:
					imageComponent.sprite = null;
					break;
				case Sprite sprite:
					imageComponent.sprite = sprite;
					break;
				default:
					Debug.LogError("ImageConstraint image is not a sprite: " + image, this);
					break;
			}
		}
		catch (EvaluationException e) {
			Debug.LogException(e, this);
		}
	}
}
}