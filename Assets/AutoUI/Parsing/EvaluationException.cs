using System;
using AutoUI.Parsing.Expressions;

namespace AutoUI.Parsing {
public class EvaluationException : Exception {
	public EvaluationException(string message, Expression expressionToEvaluate) : base(message) {
		ExpressionToEvaluate = expressionToEvaluate;
	}

	public Expression ExpressionToEvaluate { get; }
}
}