using System;
using System.Collections.Generic;

public class EvaluationException : Exception
{
    public Value ValueToEvaluate { get; private set; }

    public EvaluationException(string message, Value valueToEvaluate) : base(message)
    {
        ValueToEvaluate = valueToEvaluate;
    }
}