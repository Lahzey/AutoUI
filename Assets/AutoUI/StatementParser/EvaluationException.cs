using System;
using System.Collections.Generic;

public class EvaluationException : Exception
{
    public string Message { get; private set; }
    public Value ValueToEvaluate { get; private set; }

    public EvaluationException(string message, Value valueToEvaluate)
    {
        Message = message;
        ValueToEvaluate = valueToEvaluate;
    }
}