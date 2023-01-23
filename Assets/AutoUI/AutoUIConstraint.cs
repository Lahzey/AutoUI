using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public class AutoUIConstraint : MonoBehaviour
{

    [SerializeField] private string statement;
    [SerializeField] private string output;
    
    // Start is called before the first frame update
    void Start()
    {
        try
        {
            Value value = ValuePatternMatcher.Parse(statement, out ParseResult parseResult);
            output = $"[{value.GetType().Name}]: {value.Evaluate()}";
        }
        catch (ParseException e)
        {
            output = "";
            for (int i = 0; i < e.MessageCount(); i++)
            {
                output += (i+1) + ": "+ e.GetMessage(i) + " at " + e.GetStartPosition(i) + "\n";
            }
        }
        
    }

    // Update is called once per frame
    void Update()
    {
    }
}
