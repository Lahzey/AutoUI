using System;
using System.Collections.Generic;

public class EvaluationException : Exception
{
    public Expression ExpressionToEvaluate { get; private set; }

    public EvaluationException(string message, Expression expressionToEvaluate) : base(message)
    {
        ExpressionToEvaluate = expressionToEvaluate;
    }
}