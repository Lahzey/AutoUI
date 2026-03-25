using System;
using System.Collections.Generic;
using System.Linq;
using AutoUI.Parsing.Expressions;

namespace AutoUI.Parsing {
public class CodeParser {
	private static readonly object INIT_LOCK = new object();

	private static Dictionary<string, Type> valueTypePatterns;
	private static Dictionary<string, int> valueTypePatternPriorities;
	private static List<string> patternOrder;
	private static string uid = Guid.NewGuid().ToString();

	private static void Init() {
		IEnumerable<Type> subclasses =
			from assembly in AppDomain.CurrentDomain.GetAssemblies()
			from type in assembly.GetTypes()
			where type.IsSubclassOf(typeof(Expression))
			select type;
		valueTypePatterns = new Dictionary<string, Type>();
		valueTypePatternPriorities = new Dictionary<string, int>();
		foreach (Type type in subclasses)
			// get ValuePattern attribute from type
		foreach (ExpressionPatternAttribute patternAttribute in type.GetCustomAttributes(typeof(ExpressionPatternAttribute), false)) {
			string pattern = patternAttribute.GetPattern();
			string[] patternArray = pattern.Split(' ');
			if (valueTypePatterns.ContainsKey(pattern))
				throw new ArgumentException($"Duplicate use of pattern {pattern} by {type.Name} and {valueTypePatterns[pattern].Name}.");
			valueTypePatterns.Add(pattern, type);

			// get priority of pattern from PatternPriority attribute (or 0 if not specified)
			int priority = 0;
			type.GetCustomAttributes(typeof(PatternPriorityAttribute), false).ToList().ForEach(a => priority = ((PatternPriorityAttribute)a).priority);
			valueTypePatternPriorities.Add(pattern, priority);
		}

		patternOrder = valueTypePatterns.Keys.ToList();
		patternOrder = patternOrder.OrderByDescending(i => valueTypePatternPriorities[i]).ToList();
	}

	public static ParseResult TryParse(string valueString) {
		lock (INIT_LOCK) {
			if (valueTypePatterns == null) Init();
		}

		ParseResult parseResult = Tokenizer.Tokenize(valueString);

		while (true)
			if (!PatternMatch.MatchNext(parseResult, valueTypePatterns, patternOrder))
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
				parseResult.AddExceptionMessage("Cannot have more than one expression in a expression expression", parseResult.GetSourceStartIndex(1), parseResult.source.Length);
		}

		return parseResult;
	}
}
}