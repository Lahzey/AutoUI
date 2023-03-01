using System;
using AutoUI.CodeParser.Expressions;

namespace AutoUI.CodeParser {
public class EvaluationException : Exception {
	public EvaluationException(string message, Expression expressionToEvaluate) : base(message) {
		ExpressionToEvaluate = expressionToEvaluate;
	}

	public Expression ExpressionToEvaluate { get; }
}
}