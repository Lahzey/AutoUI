using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class RectTransformConstraint : AutoUIConstraint
{
    private RectTransform rectTransform;
    
    // anchor constraints
    [SerializeField] private string minXConstraint;
    [SerializeField] private string maxXConstraint;
    [SerializeField] private string minYConstraint;
    [SerializeField] private string maxYConstraint;
    
    private int[] constraintIndexes = new int[4];

    protected override void Awake()
    {
        base.Awake();
        rectTransform = GetComponent<RectTransform>();
        
        // add all the expression inputs
        int i = 0;
        int constraintIndex = 0;
        // order here has to match order in used in update
        foreach (string constraint in new string[] {minXConstraint, maxXConstraint, minYConstraint, maxYConstraint})
        {
            if (constraint.Trim().Length > 0)
            {
                AddValueInput(constraint);
                constraintIndexes[i] = constraintIndex;
                constraintIndex++;
            } else constraintIndexes[i] = -1;

            i++;
        }
    }

    // Update is called once per frame
    public override void Render(DataContext context)
    {
        float minX = rectTransform.anchorMin.x;
        float maxX = rectTransform.anchorMax.x;
        float minY = rectTransform.anchorMin.y;
        float maxY = rectTransform.anchorMax.y;
        
        if (constraintIndexes[0] >= 0)
        {
            object value = values[constraintIndexes[0]].Evaluate(context);
            if (value is short or int or long or float or double) minX = (float)value;
            else Debug.LogError("Min X Constraint '" + minXConstraint + "' does not evaluate to a number. Result: " + value);
        }
        
        if (constraintIndexes[1] >= 0)
        {
            object value = values[constraintIndexes[1]].Evaluate(context);
            if (value is short or int or long or float or double) maxX = (float)value;
            else Debug.LogError("Max X Constraint '" + maxXConstraint + "' does not evaluate to a number. Result: " + value);
        }
        
        if (constraintIndexes[2] >= 0)
        {
            object value = values[constraintIndexes[2]].Evaluate(context);
            if (value is short or int or long or float or double) minY = (float)value;
            else Debug.LogError("Min Y Constraint '" + minYConstraint + "' does not evaluate to a number. Result: " + value);
        }
        
        if (constraintIndexes[3] >= 0)
        {
            object value = values[constraintIndexes[3]].Evaluate(context);
            if (value is short or int or long or float or double) maxY = (float)value;
            else Debug.LogError("Max Y Constraint '" + maxYConstraint + "' does not evaluate to a number. Result: " + value);
        }
        
        rectTransform.anchorMin = new Vector2(minX, minY);
        rectTransform.anchorMax = new Vector2(maxX, maxY);
    }
    
    
}