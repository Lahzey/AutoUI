using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace AutoUI.Parsing {
public abstract class Token : ParsedElement {
	public static Token Get(string tokenString) {
		Token[] tokens = tokenString.Length == 1 ? SingleCharToken.VALUES : MultiCharToken.VALUES;
		foreach (Token token in tokens)
			if (token.ToString().Equals(tokenString))
				return token;

		return null;
	}
}

public class IdentifierToken : Token {
	private static readonly string[] KEYWORDS = { "if", "else", "while", "for", "return", "break", "continue", "int", "float", "bool", "string", "void", "true", "false" };

	public IdentifierToken(string identifier) {
		Identifier = identifier;
	}

	public string Identifier { get; }

	public bool IsKeyword() {
		return KEYWORDS.Contains(Identifier);
	}

	public override string ToString() {
		return "I";
	}

	public override string GetPlaceholder() {
		// returns the identifier itself if it's a keyword, otherwise (for example for variable names) it returns the base placeholder, which shows the type hierarchy of the token
		return IsKeyword() ? Identifier : base.GetPlaceholder();
	}
}

public class StringToken : Token {
	public StringToken(string value) {
		Value = value;
	}

	public string Value { get; }

	public override string ToString() {
		return "S";
	}
}

public class NumberToken : Token {
	public NumberToken(object value) {
		Value = value;
	}

	public object Value { get; } // can be any type of number (int, float, double, etc.)

	public override string ToString() {
		return "N";
	}

	public static void ReplaceTokens(ParseResult parseResult) {
		for (int i = 0; i < parseResult.Count; i++) {
			if (parseResult[i] is not IdentifierToken) continue;

			IdentifierToken identifierToken = (IdentifierToken)parseResult[i];

			bool floatingPoint = false;
			Match match = Regex.Match(identifierToken.Identifier, @"^([0-9]+)([lLfFdD]?)$");
			if (!match.Success) {
				match = Regex.Match(identifierToken.Identifier, @"^([0-9]*\.[0-9]+)([lLfFdD]?)$");
				if (!match.Success)
					continue;
				floatingPoint = true;
			}

			char suffix = match.Groups[2].Value.Length > 0 ? match.Groups[2].Value[0] : ' ';
			string numberString = match.Groups[1].Value;
			object number;
			switch (suffix) {
				case 'l':
				case 'L':
					number = long.Parse(numberString);
					break;
				case 'f':
				case 'F':
					number = float.Parse(numberString);
					break;
				case 'd':
				case 'D':
					number = double.Parse(numberString);
					break;
				case ' ':
					if (floatingPoint) {
						double doubleNumber = double.Parse(numberString);
						number = doubleNumber is <= float.MaxValue and >= float.MinValue ? (float)doubleNumber : doubleNumber;
					}
					else {
						long longNumber = long.Parse(numberString);
						number = longNumber is <= int.MaxValue and >= int.MinValue ? (int)longNumber : longNumber;
					}

					break;
				default:
					parseResult.AddExceptionMessage(suffix + " is not a valid number suffix", parseResult.GetSourceStartIndex(i), parseResult.GetSourceEndIndex(i));
					return;
			}

			parseResult.Replace(i, new NumberToken(number));
		}
	}
}

public class WhitespaceToken : Token {
	public static char placeholder = 'W';

	private char whitespace;

	public WhitespaceToken(char whitespace) {
		this.whitespace = whitespace;
	}

	public override string ToString() {
		return placeholder + "";
	}

	public override string GetPlaceholder() {
		return placeholder + "";
	}
}

public class SingleCharToken : Token {
	public static SingleCharToken plus = new SingleCharToken('+');
	public static SingleCharToken minus = new SingleCharToken('-');
	public static SingleCharToken mult = new SingleCharToken('*');
	public static SingleCharToken divide = new SingleCharToken('/');
	public static SingleCharToken lparen = new SingleCharToken('(');
	public static SingleCharToken rparen = new SingleCharToken(')');
	public static SingleCharToken assign = new SingleCharToken('=');
	public static SingleCharToken greater = new SingleCharToken('>');
	public static SingleCharToken smaller = new SingleCharToken('<');
	public static SingleCharToken and = new SingleCharToken('&');
	public static SingleCharToken or = new SingleCharToken('|');
	public static SingleCharToken not = new SingleCharToken('!');
	public static SingleCharToken dot = new SingleCharToken('.');
	public static SingleCharToken cond = new SingleCharToken('?');
	public static SingleCharToken alt = new SingleCharToken(':');

	internal static readonly SingleCharToken[] VALUES = { plus, minus, mult, divide, lparen, rparen, assign, greater, smaller, and, or, not, dot, cond, alt };

	public readonly char c;

	private SingleCharToken(char c) {
		this.c = c;
	}

	public static SingleCharToken Get(char c) {
		foreach (SingleCharToken value in VALUES)
			if (value.c == c)
				return value;
		return null;
	}

	public override string ToString() {
		return "" + c;
	}

	public override string GetPlaceholder() {
		return "" + c;
	}
}

public class MultiCharToken : Token {
	public static MultiCharToken equals = new MultiCharToken(SingleCharToken.assign, SingleCharToken.assign);
	public static MultiCharToken notEquals = new MultiCharToken(SingleCharToken.not, SingleCharToken.assign);
	public static MultiCharToken logicalAnd = new MultiCharToken(SingleCharToken.and, SingleCharToken.and);
	public static MultiCharToken logicalOr = new MultiCharToken(SingleCharToken.or, SingleCharToken.or);
	public static MultiCharToken greaterOrEqual = new MultiCharToken(SingleCharToken.greater, SingleCharToken.assign);
	public static MultiCharToken smallerOrEqual = new MultiCharToken(SingleCharToken.smaller, SingleCharToken.assign);

	internal static readonly MultiCharToken[] VALUES = { equals, notEquals, logicalAnd, logicalOr, greaterOrEqual, smallerOrEqual };

	private readonly SingleCharToken[] tokens;

	private MultiCharToken(params SingleCharToken[] tokens) {
		this.tokens = tokens;
	}

	public static void ReplaceTokens(ParseResult parseResult) {
		// create a text representation of this token array so we can easy find and replace tokens with MultiCharTokens using string replace
		string tokenString = "";
		foreach (Token token in parseResult) // at this point every matchable is a token
		{
			string tokenStringRepresentation = token.ToString();
			tokenString += token.ToString();
		}

		foreach (MultiCharToken multiCharToken in VALUES) {
			string id = multiCharToken.ToString();
			int index = tokenString.IndexOf(id, StringComparison.Ordinal);
			while (index != -1) {
				parseResult.ReplaceRange(index, id.Length, multiCharToken);
				index = tokenString.IndexOf(id, index + id.Length, StringComparison.Ordinal);
			}
		}
	}

	public override string ToString() {
		string text = "";
		foreach (SingleCharToken token in tokens) text += token.ToString();
		return text;
	}

	public override string GetPlaceholder() {
		return ToString();
	}
}
}