using System;
using System.Collections.Generic;
using System.Reflection;
using AutoUI.Data;

namespace AutoUI.Parsing.Expressions {
public abstract class Expression : ParsedElement {
	protected object[] args;

	public virtual void Prepare(object[] args) {
		this.args = args;
	}

	public virtual bool HidesInspectedChildren() // if true, the inspector will show the information of this expression instead of the child at that position
	{
		return false;
	}

	// the type that can be expected from this expression before it is executed, usually just object
	public virtual Type GetExpectedType() {
		return typeof(object);
	}

	public abstract object Evaluate(DataContext context);
}

[ExpressionPattern(typeof(IdentifierToken))]
[PatternPriority(999)]
public class VariableExpression : Expression {
	// can be set by other expressions to let it know that this variable is a field or property of the given type and not an access of the data store
	public Type contextType;

	public string VariableName => ((IdentifierToken)args[0]).Identifier;

	public override object Evaluate(DataContext context) {
		return context.Get(VariableName);
	}

	public override Type GetExpectedType() {
		if (contextType == null) return DataKeyBase.Get(VariableName)?.Type ?? typeof(object);
		if (contextType == typeof(object)) return typeof(object);
		return contextType.GetField(VariableName)?.FieldType ?? contextType.GetProperty(VariableName)?.PropertyType ?? typeof(object);
	}

	public bool? IsValid() {
		if (contextType == null) return DataKeyBase.Get(VariableName) != null ? true : null; // data store can contain unlisted keys, so we never mark these as invalid
		if (contextType == typeof(object)) return null; // context type object means unknown, so we cannot know if the variable is valid
		return contextType.GetField(VariableName) != null || contextType.GetProperty(VariableName) != null;
	}
}

[ExpressionPattern(@"\(", typeof(Expression), @"\)")]
[PatternPriority(999)]
public class BracketExpression : Expression {
	public override object Evaluate(DataContext context) {
		return ((Expression)args[0]).Evaluate(context);
	}

	public override Type GetExpectedType() {
		return ((Expression)args[0]).GetExpectedType();
	}
}

[ExpressionPattern(typeof(StringToken))]
[PatternPriority(999)]
public class StringExpression : Expression {
	public override object Evaluate(DataContext context) {
		return ((StringToken)args[0]).Value;
	}

	public override Type GetExpectedType() {
		return typeof(string);
	}
}

[ExpressionPattern(typeof(NumberToken))]
[PatternPriority(999)]
public class NumberExpression : Expression {
	public override object Evaluate(DataContext context) {
		return ((NumberToken)args[0]).Value;
	}

	public override Type GetExpectedType() {
		return ((NumberToken)args[0]).Value.GetType();
	}
}

[ExpressionPattern(typeof(Expression), @"\?", typeof(Expression), @":", typeof(Expression))]
[PatternPriority(-999)]
public class ConditionalExpression : Expression {
	public Expression Condition => (Expression)args[0];
	public Expression FirstExpression => (Expression)args[1];
	public Expression SecondExpression => (Expression)args[2];

	public override object Evaluate(DataContext context) {
		object ConditionValue = Condition.Evaluate(context);
		if (ConditionValue == null) return FirstExpression.Evaluate(context);
		if (ConditionValue is bool b) return b ? FirstExpression.Evaluate(context) : SecondExpression.Evaluate(context);
		throw new EvaluationException("ConditionalExpression condition must be a boolean", this);
	}
}

[ExpressionPattern(typeof(Expression), @" \. (", typeof(VariableExpression), @"(?: \. ", typeof(VariableExpression), ")*)", AutoSpace = false)]
[PatternPriority(1)]
public class FieldAccessExpression : Expression {
	public Expression Object => (Expression)args[0];
	public List<object> FieldNames => (List<object>)args[1];

	public override void Prepare(object[] args) {
		base.Prepare(args);
		Type objectType = typeof(object);
		Type contextType = Object.GetExpectedType();
		foreach (object fieldNameExpression in FieldNames) {
			((VariableExpression)fieldNameExpression).contextType = contextType;
			contextType = ((VariableExpression)fieldNameExpression).GetExpectedType();
		}
	}

	public override object Evaluate(DataContext context) {
		object objectValue = Object.Evaluate(context);

		foreach (object fieldNameExpression in FieldNames) {
			string fieldName = ((VariableExpression)fieldNameExpression).VariableName;
			if (objectValue == null) throw new EvaluationException("Trying to access field of null object", this);
			Type type = objectValue.GetType();

			FieldInfo fieldInfo = type.GetField(fieldName);
			if (fieldInfo != null) {
				objectValue = fieldInfo.GetValue(objectValue);
				continue;
			}

			PropertyInfo propertyInfo = type.GetProperty(fieldName);
			if (propertyInfo != null) {
				objectValue = propertyInfo.GetValue(objectValue);
				continue;
			}

			throw new EvaluationException($"Field or property '{fieldName}' not found for type '{type.FullName}'", this);
		}

		return objectValue;
	}
}

// probably should not support field assignment, but leaving this here if needed in the future
// [ValuePattern(typeof(FieldAccessExpression), @"=", typeof(Expression)), PatternPriority(-2)]
// public class FieldAssignmentValue : Expression
// {
//     public override object Evaluate(DataContext context)
//     {
//         object ObjectValue = ((FieldAccessExpression)args[0]).Object.Evaluate(context);
//         string FieldName = ((FieldAccessExpression)args[0]).FieldName;
//         object Expression = ((Expression)args[1]).Evaluate(context);
//         ObjectValue.GetType().GetField(FieldName).SetValue(ObjectValue, Expression);
//         return Expression;
//     }
//
//     public Expression Expression => (Expression) args[1];
// }
}