using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace AutoUI.Parsing {
[AttributeUsage(AttributeTargets.Class)]
public class ExpressionPatternAttribute : Attribute {
	private readonly object[] patternParts;

	public ExpressionPatternAttribute(params object[] patternParts) {
		this.patternParts = patternParts;
	}

	public bool AutoSpace { get; set; } = true;

	public string GetPattern() {
		List<string> pattern = new();
		foreach (object patternPart in patternParts)
			if (patternPart is Type type) pattern.Add(TypeOfPattern(type));
			else if (patternPart is string text) pattern.Add(text);
			else throw new ArgumentException("ExpressionPatternAttribute only accepts strings and types as arguments.");

		string joined = string.Join(AutoSpace ? " " : "", pattern);
		StringBuilder resultBuilder = new();
		foreach (char c in joined)
			if (c == ' ') resultBuilder.Append(@" (?:" + WhitespaceToken.PLACEHOLDER + @" )*");
			else resultBuilder.Append(c);
		return resultBuilder.ToString();
	}

	public static string TypeOfPattern(Type type) {
		// return a regex pattern that matches the type plus any additional type names after a trailing '>' that would denote a subtype
		return Regex.Escape(TypeOfPlaceholder(type)) + @"(?:>[a-zA-Z0-9_\.]+)?";
	}

	public static string TypeOfPlaceholder(Type type) {
		string typeHierarchy = type.FullName;
		while (type.BaseType != null) {
			type = type.BaseType;
			typeHierarchy = type.FullName + ">" + typeHierarchy;
		}

		return "$" + typeHierarchy;
	}
}

[AttributeUsage(AttributeTargets.Class)]
public class PatternPriorityAttribute : Attribute {
	public readonly int Priority;

	public PatternPriorityAttribute(int priority) {
		Priority = priority;
	}
}
}