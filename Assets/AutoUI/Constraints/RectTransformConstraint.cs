using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class RectTransformConstraint : AutoUIConstraint
{
    private RectTransform rectTransform;
    
    // anchor constraints
    [SerializeField] private CodeInput minXConstraint;
    [SerializeField] private CodeInput maxXConstraint;
    [SerializeField] private CodeInput minYConstraint;
    [SerializeField] private CodeInput maxYConstraint;

    private CodeInput[] constraints => new CodeInput[] { minXConstraint, maxXConstraint, minYConstraint, maxYConstraint };
    
    private Dictionary<string, Expression> expressions = new Dictionary<string, Expression>();

    protected override void Awake()
    {
        base.Awake();
        rectTransform = GetComponent<RectTransform>();
        
        foreach (CodeInput constraint in constraints)
        {
            ParseResult parseResult = constraint.Result;
            if (constraint.Input.Length == 0 || parseResult is not { Success: true }) Debug.LogError("Failed to RectTransformConstraint expression '" + constraint.Input + "'.", this);
            else expressions.Add(constraint.Input, parseResult.Expression);
        }
    }

    // Update is called once per frame
    public override void Render(DataContext context)
    {
        // Values here must be in the same order as defined by the constraints property. Sorry, I know its not clean :(
        float[] values = new float[4];
        values[0] = rectTransform.anchorMin.x;
        values[1] = rectTransform.anchorMax.x;
        values[2] = rectTransform.anchorMin.y;
        values[3] = rectTransform.anchorMax.y;

        for (int i = 0; i < constraints.Length; i++)
        {
            CodeInput constraint = constraints[i];
            if (expressions.ContainsKey(constraint.Input))
            {
                object value = expressions[constraint.Input].Evaluate(context);
                if (value is short or int or long or float or double) values[i] = (float)value;
                else Debug.LogError("RectTransformConstraint expression '" + constraint.Input + "' does not evaluate to a number. Result: " + value);
            }
        }
        
        rectTransform.anchorMin = new Vector2(values[0], values[2]);
        rectTransform.anchorMax = new Vector2(values[1], values[3]);
    }
    
    
}