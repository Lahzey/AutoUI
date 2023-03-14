using System;
using System.Collections.Generic;
using System.Linq;
using AutoUI.Parsing.Expressions;

namespace AutoUI.Parsing {
public class CodeParser {
	private static readonly object INIT_LOCK = new();

	private static Dictionary<string, Type> ValueTypePatterns;
	private static Dictionary<string, int> ValueTypePatternPriorities;
	private static List<string> PatternOrder;
	private static string uid = Guid.NewGuid().ToString();

	private static void init() {
		IEnumerable<Type> subclasses =
			from assembly in AppDomain.CurrentDomain.GetAssemblies()
			from type in assembly.GetTypes()
			where type.IsSubclassOf(typeof(Expression))
			select type;
		ValueTypePatterns = new Dictionary<string, Type>();
		ValueTypePatternPriorities = new Dictionary<string, int>();
		foreach (Type type in subclasses)
			// get ValuePattern attribute from type
		foreach (ExpressionPatternAttribute patternAttribute in type.GetCustomAttributes(typeof(ExpressionPatternAttribute), false)) {
			string pattern = patternAttribute.GetPattern();
			string[] patternArray = pattern.Split(' ');
			if (ValueTypePatterns.ContainsKey(pattern))
				throw new ArgumentException($"Duplicate use of pattern {pattern} by {type.Name} and {ValueTypePatterns[pattern].Name}.");
			ValueTypePatterns.Add(pattern, type);

			// get priority of pattern from PatternPriority attribute (or 0 if not specified)
			int priority = 0;
			type.GetCustomAttributes(typeof(PatternPriorityAttribute), false).ToList().ForEach(a => priority = ((PatternPriorityAttribute)a).Priority);
			ValueTypePatternPriorities.Add(pattern, priority);
		}

		PatternOrder = ValueTypePatterns.Keys.ToList();
		PatternOrder = PatternOrder.OrderByDescending(i => ValueTypePatternPriorities[i]).ToList();
	}

	public static ParseResult TryParse(string valueString) {
		lock (INIT_LOCK) {
			if (ValueTypePatterns == null) init();
		}

		ParseResult parseResult = Tokenizer.Tokenize(valueString);

		while (true)
			if (!PatternMatch.MatchNext(parseResult, ValueTypePatterns, PatternOrder))
				break;

		if (parseResult.Count > 1 || (parseResult.Count == 1 && parseResult[0] is not Expression)) {
			bool hasUnresolvedTokens = false;
			for (int i = 0; i < parseResult.Count; i++)
				if (parseResult[i] is not Expression) {
					parseResult.AddExceptionMessage("Unresolved token", parseResult.GetSourceStartIndex(i), parseResult.GetSourceEndIndex(i));
					hasUnresolvedTokens = true;
				}

			if (!hasUnresolvedTokens)
				// if we only have values in the parse result, but there are more than one
				parseResult.AddExceptionMessage("Cannot have more than one expression in a expression expression", parseResult.GetSourceStartIndex(1), parseResult.Source.Length);
		}

		return parseResult;
	}
}
}