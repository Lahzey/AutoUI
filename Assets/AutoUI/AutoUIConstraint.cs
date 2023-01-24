using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class AutoUIConstraint : MonoBehaviour
{

    [SerializeField] private string statement;
    [SerializeField] private string output;
    
    // Start is called before the first frame update
    void Start()
    {
        try
        {
            long start = Stopwatch.GetTimestamp();
            Value value = ValuePatternMatcher.Parse(statement, out ParseResult parseResult);
            output = value.Evaluate(DataStore.Instance) + "";
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
