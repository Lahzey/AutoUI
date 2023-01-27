using System;
using TMPro;
using UnityEngine;

public class ShowConstraint : AutoUIConstraint
{

    [SerializeField] private string condition;
    [SerializeField] private bool disableAtRuntime; // since this constraint works even if disabled, we need another way of disabling it


    protected override void Awake()
    {
        base.Awake();
        AddValueInput(condition);
    }

    public override void Render(DataContext context)
    {
        if (disableAtRuntime) return;
        try
        {
            object result = values[0].Evaluate(context);
            if (result == null)
                gameObject.SetActive(false);
            else if (result is bool)
                gameObject.SetActive((bool) result);
            else
                Debug.LogError("Show condition not a boolean: " + result, this);
        }
        catch (EvaluationException e)
        {
            Debug.LogException(e, this);
        }
    }
}