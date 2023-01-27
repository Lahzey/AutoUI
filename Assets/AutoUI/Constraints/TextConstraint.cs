using System;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
public class TextConstraint : AutoUIConstraint
{

    [SerializeField] private string textConstraint;
    private TextMeshProUGUI text;
    
    protected override void Awake()
    {
        base.Awake();
        text = GetComponent<TextMeshProUGUI>();
        AddValueInput(textConstraint);
    }

    public override void Render(DataContext context)
    {
        try
        {
            text.text = values[0].Evaluate(context)?.ToString() ?? "null";
        }
        catch (EvaluationException e)
        {
            Debug.LogException(e, this);
        }
    }
}