using System;
using System.Collections.Generic;
using System.Threading;
using AutoUI.Parsing;
using UnityEngine.Serialization;

namespace AutoUI.Inspector {
[Serializable]
public class CodeInput {
	// we don't want to serialize parse results, and we also don't need to parse given string more than once
	private static readonly Dictionary<string, ParseResult> PARSE_RESULTS = new Dictionary<string, ParseResult>();

	private static readonly List<string> CURRENTLY_PARSING = new List<string>();

	[FormerlySerializedAs("Input")] public string input = "";
	public ParseResult Result => GetParseResultAwait(input);

    /// <summary>
    ///     Get the parse result for a given input string, or wait for it to be parsed if it's not already parsed.<br />
    ///     THIS WILL BLOCK NOT ONLY THE CURRENT THREAD, BUT ALSO ANY OTHER THREAD TRYING TO GET A PARSE RESULT, DO NOT USE
    ///     FROM UI!
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    private static ParseResult GetParseResultAwait(string input) {
		lock (PARSE_RESULTS) {
			if (!PARSE_RESULTS.ContainsKey(input)) PARSE_RESULTS.Add(input, CodeParser.TryParse(input));
			return PARSE_RESULTS[input];
		}
	}


	public static ParseResult GetParseResult(string input, Action<ParseResult> onResult = null) {
		lock (PARSE_RESULTS) {
			if (PARSE_RESULTS.ContainsKey(input)) {
				onResult?.Invoke(PARSE_RESULTS[input]);
				return PARSE_RESULTS[input];
			}
		}

		ParseAsync(input, onResult);
		return null;
	}

	public static void ParseAsync(string input, Action<ParseResult> onResult = null) {
		lock (CURRENTLY_PARSING) {
			if (CURRENTLY_PARSING.Contains(input)) return; // we cannot call onResult here, but that's fine. The UI will usually be the first to request a parse result and if it doesn't work sometimes it's okay
			CURRENTLY_PARSING.Add(input);
		}

		Thread thread = new Thread(() => {
            ParseResult result = CodeParser.TryParse(input);
            onResult?.Invoke(result);
            lock (PARSE_RESULTS) {
                if (!PARSE_RESULTS.ContainsKey(input)) PARSE_RESULTS.Add(input, result); // the contains key check should be redundant, but just in case
            }

            lock (CURRENTLY_PARSING) {
                CURRENTLY_PARSING.Remove(input);
            }
        });
		thread.Start();
	}
}
}