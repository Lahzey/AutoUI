using System;
using AutoUI.Data;

namespace AutoUI.Parsing.Expressions {
[ExpressionPattern(typeof(Expression), @"==", typeof(Expression))]
[PatternPriority(-100)]
public class EqualsExpression : Expression {
	public Expression Left => (Expression)args[0];
	public Expression Right => (Expression)args[1];

	public override object Evaluate(DataContext context) {
		object left = Left.Evaluate(context);
		object right = Right.Evaluate(context);
		return left == right;
	}
}

[ExpressionPattern(typeof(Expression), @"!=", typeof(Expression))]
[PatternPriority(-100)]
public class NotEqualsExpression : Expression {
	public Expression Left => (Expression)args[0];
	public Expression Right => (Expression)args[1];

	public override object Evaluate(DataContext context) {
		object left = Left.Evaluate(context);
		object right = Right.Evaluate(context);
		return left != right;
	}
}

[ExpressionPattern(typeof(Expression), @">", typeof(Expression))]
[PatternPriority(-100)]
public class GreaterThanExpression : Expression {
	public Expression Left => (Expression)args[0];
	public Expression Right => (Expression)args[1];

	public override object Evaluate(DataContext context) {
		object left = Left.Evaluate(context);
		object right = Right.Evaluate(context);
		if (left is short or int or long or float or double && right is short or int or long or float or double)
			return Convert.ToDouble(left) > Convert.ToDouble(right);
		throw new EvaluationException("Cannot perform > on non-numeric values", this);
	}
}

[ExpressionPattern(typeof(Expression), @">=", typeof(Expression))]
[PatternPriority(-100)]
public class GreaterThanOrEqualExpression : Expression {
	public Expression Left => (Expression)args[0];
	public Expression Right => (Expression)args[1];

	public override object Evaluate(DataContext context) {
		object left = Left.Evaluate(context);
		object right = Right.Evaluate(context);
		if (left is short or int or long or float or double && right is short or int or long or float or double)
			return Convert.ToDouble(left) >= Convert.ToDouble(right);
		throw new EvaluationException("Cannot perform >= on non-numeric values", this);
	}
}

[ExpressionPattern(typeof(Expression), @"<", typeof(Expression))]
[PatternPriority(-100)]
public class SmallerThanExpression : Expression {
	public Expression Left => (Expression)args[0];
	public Expression Right => (Expression)args[1];

	public override object Evaluate(DataContext context) {
		object left = Left.Evaluate(context);
		object right = Right.Evaluate(context);
		if (left is short or int or long or float or double && right is short or int or long or float or double)
			return Convert.ToDouble(left) < Convert.ToDouble(right);
		throw new EvaluationException("Cannot perform < on non-numeric values", this);
	}
}

[ExpressionPattern(typeof(Expression), @"<=", typeof(Expression))]
[PatternPriority(-100)]
public class SmallerThanOrEqualExpression : Expression {
	public Expression Left => (Expression)args[0];
	public Expression Right => (Expression)args[1];

	public override object Evaluate(DataContext context) {
		object left = Left.Evaluate(context);
		object right = Right.Evaluate(context);
		if (left is short or int or long or float or double && right is short or int or long or float or double)
			return Convert.ToDouble(left) <= Convert.ToDouble(right);
		throw new EvaluationException("Cannot perform <= on non-numeric values", this);
	}
}

[ExpressionPattern(typeof(Expression), @"&", typeof(Expression))]
[PatternPriority(-101)]
public class AndExpression : Expression {
	public Expression Left => (Expression)args[0];
	public Expression Right => (Expression)args[1];

	public override object Evaluate(DataContext context) {
		object left = Left.Evaluate(context);
		object right = Right.Evaluate(context);
		if (!(left is bool) || !(right is bool))
			throw new EvaluationException("Cannot perform AND on non-boolean values", this);
		return (bool)left & (bool)right;
	}
}

[ExpressionPattern(typeof(Expression), @"&&", typeof(Expression))]
[PatternPriority(-101)]
public class LogicalAndExpression : Expression {
	public Expression Left => (Expression)args[0];
	public Expression Right => (Expression)args[1];

	public override object Evaluate(DataContext context) {
		object left = Left.Evaluate(context);
		object right = Right.Evaluate(context);
		if (!(left is bool) || !(right is bool))
			throw new EvaluationException("Cannot perform AND on non-boolean values", this);
		return (bool)left && (bool)right;
	}
}

[ExpressionPattern(typeof(Expression), @"\|", typeof(Expression))]
[PatternPriority(-102)]
public class OrExpression : Expression {
	public Expression Left => (Expression)args[0];
	public Expression Right => (Expression)args[1];

	public override object Evaluate(DataContext context) {
		object left = Left.Evaluate(context);
		object right = Right.Evaluate(context);
		if (!(left is bool) || !(right is bool))
			throw new EvaluationException("Cannot perform OR on non-boolean values", this);
		return (bool)left | (bool)right;
	}
}

[ExpressionPattern(typeof(Expression), @"\|\|", typeof(Expression))]
[PatternPriority(-102)]
public class LogicalOrExpression : Expression {
	public Expression Left => (Expression)args[0];
	public Expression Right => (Expression)args[1];

	public override object Evaluate(DataContext context) {
		object left = Left.Evaluate(context);
		object right = Right.Evaluate(context);
		if (!(left is bool) || !(right is bool))
			throw new EvaluationException("Cannot perform OR on non-boolean values", this);
		return (bool)left || (bool)right;
	}
}

[ExpressionPattern(@"!", typeof(Expression))]
[PatternPriority(-103)]
public class NotExpression : Expression {
	public Expression Expression => (Expression)args[0];

	public override object Evaluate(DataContext context) {
		object value = Expression.Evaluate(context);
		if (!(value is bool))
			throw new EvaluationException("Cannot perform NOT on non-boolean values", this);
		return !(bool)value;
	}
}
}