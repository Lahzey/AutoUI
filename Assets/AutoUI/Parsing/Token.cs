using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace AutoUI.Parsing {
public abstract class Token : ParsedElement {
	public static Token Get(string tokenString) {
		Token[] tokens = tokenString.Length == 1 ? SingleCharToken.Values : MultiCharToken.Values;
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
	public static char PLACEHOLDER = 'W';

	private char whitespace;

	public WhitespaceToken(char whitespace) {
		this.whitespace = whitespace;
	}

	public override string ToString() {
		return PLACEHOLDER + "";
	}

	public override string GetPlaceholder() {
		return PLACEHOLDER + "";
	}
}

public class SingleCharToken : Token {
	public static SingleCharToken PLUS = new('+');
	public static SingleCharToken MINUS = new('-');
	public static SingleCharToken MULT = new('*');
	public static SingleCharToken DIVIDE = new('/');
	public static SingleCharToken LPAREN = new('(');
	public static SingleCharToken RPAREN = new(')');
	public static SingleCharToken ASSIGN = new('=');
	public static SingleCharToken GREATER = new('>');
	public static SingleCharToken SMALLER = new('<');
	public static SingleCharToken AND = new('&');
	public static SingleCharToken OR = new('|');
	public static SingleCharToken NOT = new('!');
	public static SingleCharToken DOT = new('.');
	public static SingleCharToken COND = new('?');
	public static SingleCharToken ALT = new(':');

	internal static readonly SingleCharToken[] Values = { PLUS, MINUS, MULT, DIVIDE, LPAREN, RPAREN, ASSIGN, GREATER, SMALLER, AND, OR, NOT, DOT, COND, ALT };

	public readonly char c;

	private SingleCharToken(char c) {
		this.c = c;
	}

	public static SingleCharToken get(char c) {
		foreach (SingleCharToken value in Values)
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
	public static MultiCharToken EQUALS = new(SingleCharToken.ASSIGN, SingleCharToken.ASSIGN);
	public static MultiCharToken NOT_EQUALS = new(SingleCharToken.NOT, SingleCharToken.ASSIGN);
	public static MultiCharToken LOGICAL_AND = new(SingleCharToken.AND, SingleCharToken.AND);
	public static MultiCharToken LOGICAL_OR = new(SingleCharToken.OR, SingleCharToken.OR);
	public static MultiCharToken GREATER_OR_EQUAL = new(SingleCharToken.GREATER, SingleCharToken.ASSIGN);
	public static MultiCharToken SMALLER_OR_EQUAL = new(SingleCharToken.SMALLER, SingleCharToken.ASSIGN);

	internal static readonly MultiCharToken[] Values = { EQUALS, NOT_EQUALS, LOGICAL_AND, LOGICAL_OR, GREATER_OR_EQUAL, SMALLER_OR_EQUAL };

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

		foreach (MultiCharToken multiCharToken in Values) {
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