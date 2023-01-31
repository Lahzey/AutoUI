using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

public abstract class AutoUIConstraint : MonoBehaviour
{
    protected UINode node;
    
    protected List<string> valueInputs = new List<string>();
    protected List<Expression> values = new List<Expression>();
    protected List<ParseResult> parseResults = new List<ParseResult>();
    protected List<ParseException> parseExceptions = new List<ParseException>();
    
    protected virtual void Awake()
    {
        node = UINode.AddToNode(this);
    }

    protected virtual void OnDestroy()
    {
        node.Remove(this);
    }

    public abstract void Render(DataContext context);

    protected void AddValueInput(string valueInput)
    {
        valueInputs.Add(valueInput);
        try
        {
            values.Add(CodeParser.Parse(valueInput, out ParseResult parseResult));
            parseResults.Add(parseResult);
            parseExceptions.Add(null);
        }
        catch (ParseException e)
        {
            values.Add(null);
            parseResults.Add(null);
            parseExceptions.Add(e);
            Debug.LogError(e);
        }
    }


}
