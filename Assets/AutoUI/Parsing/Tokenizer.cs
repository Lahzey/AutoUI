using System.Text;

namespace AutoUI.Parsing {
public class Tokenizer {
	public static ParseResult Tokenize(string source) {
		ParseResult parseResult = new(source);

		StringBuilder currentText = new();
		bool inString = false;

		for (int i = 0; i < source.Length; i++) {
			char c = source[i];
			if (c == '"') {
				// if we are in a string, we have reached the end of the string
				if (inString) {
					// add the string as a token
					parseResult.Add(new StringToken(currentText.ToString()), i + 1);
					currentText.Clear();
					inString = false;
				}
				// otherwise, we are not in a string, so we have reached the start of a string
				else {
					// if we have any text, add it as a token
					if (currentText.Length > 0) {
						parseResult.Add(new IdentifierToken(currentText.ToString()), i);
						currentText.Clear();
					}

					inString = true;
				}
			}
			else {
				// if we are in a string, add the character to the current text
				if (inString) {
					currentText.Append(c);
				}
				// otherwise, we are not in a string, so we have reached the end of a token
				else {
					if (char.IsWhiteSpace(c)) {
						// if we have any text, finalize the identifier token
						if (currentText.Length > 0) {
							parseResult.Add(new IdentifierToken(currentText.ToString()), i);
							currentText.Clear();
						}

						parseResult.Add(new WhitespaceToken(c), i + 1);
					}
					else {
						SingleCharToken token = SingleCharToken.get(c);
						if (token != null) {
							// if we have any text, finalize the identifier token
							if (currentText.Length > 0) {
								parseResult.Add(new IdentifierToken(currentText.ToString()), i);
								currentText.Clear();
							}

							// add the single character token
							parseResult.Add(token, i + 1);
						}
						else {
							// not a single char token, so we are in an identifier
							currentText.Append(c);
						}
					}
				}
			}
		}

		if (inString) parseResult.AddExceptionMessage("Unterminated string", parseResult.GetSourceStartIndex(parseResult.Count), source.Length);

		if (currentText.Length > 0) parseResult.Add(new IdentifierToken(currentText.ToString()), source.Length);

		MultiCharToken.ReplaceTokens(parseResult);
		NumberToken.ReplaceTokens(parseResult);

		return parseResult;
	}
}
}