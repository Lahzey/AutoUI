using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

public abstract class Token : ParsedElement
{
    public static Token Get(string tokenString)
    {
        Token[] tokens = tokenString.Length == 1 ? SingleCharToken.Values : MultiCharToken.Values;
        foreach (Token token in tokens)
        {
            if (token.ToString().Equals(tokenString)) return token;
        }

        return null;
    }
}

public class IdentifierToken : Token
{
    private static readonly string[] KEYWORDS = new string[] { "if", "else", "while", "for", "return", "break", "continue", "int", "float", "bool", "string", "void", "true", "false" };
    
    public string Identifier { get; private set; }
    
    public IdentifierToken(string identifier)
    {
        Identifier = identifier;
    }
    
    public bool IsKeyword()
    {
        return KEYWORDS.Contains(Identifier);
    }

    public override string ToString()
    {
        return "I";
    }

    public override string GetPlaceholder()
    {
        // returns the identifier itself if it's a keyword, otherwise (for example for variable names) it returns the base placeholder, which shows the type hierarchy of the token
        return IsKeyword() ? Identifier : base.GetPlaceholder();
    }
}

public class StringToken : Token
{
    public string Value { get; private set; }
    
    public StringToken(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return "S";
    }
}

public class NumberToken : Token
{
    public object Value { get; private set; } // can be any type of number (int, float, double, etc.)
    
    public NumberToken(object value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return "N";
    }

    public static void ReplaceTokens(ParseResult parseResult)
    {
        for (int i = 0; i < parseResult.Elements.Count; i++)
        {
            if (parseResult.Elements[i] is not IdentifierToken) continue;
            
            IdentifierToken identifierToken = (IdentifierToken) parseResult.Elements[i];
            
            bool floatingPoint = false;
            Match match = Regex.Match(identifierToken.Identifier, @"^([0-9]+)([lLfFdD]?)$");
            if (!match.Success)
            {
                match = Regex.Match(identifierToken.Identifier, @"^([0-9]*\.[0-9]+)([lLfFdD]?)$");
                if (!match.Success)
                    continue;
                floatingPoint = true;
            }
            
            char suffix = match.Groups[2].Value.Length > 0 ? match.Groups[2].Value[0] : ' ';
            string numberString = match.Groups[1].Value;
            object number;
            switch (suffix)
            {
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
                    if (floatingPoint)
                    {
                        double doubleNumber = double.Parse(numberString);
                        number = doubleNumber is <= float.MaxValue and >= float.MinValue ? (float) doubleNumber : doubleNumber;
                    }
                    else
                    {
                        long longNumber = long.Parse(numberString);
                        number = longNumber is <= int.MaxValue and >= int.MinValue ? (int) longNumber : longNumber;
                    }
                    break;
                default:
                    throw new ParseException(suffix + " is not a valid number suffix", parseResult.SourceIndexes[i], parseResult.GetSourceEndIndex(i));
            }
            
            parseResult.Elements[i] = new NumberToken(number);
        }
    }
}

public class WhitespaceToken : Token
{
    private List<char> whitespaces = new List<char>();

    public WhitespaceToken(char whitespace)
    {
        append(whitespace);
    }
    
    public void append(char whitespace)
    {
        whitespaces.Add(whitespace);
    }

    public override string ToString()
    {
        return "W";
    }
}

public class SingleCharToken : Token
{
    public static SingleCharToken PLUS = new SingleCharToken('+');
    public static SingleCharToken MINUS = new SingleCharToken('-');
    public static SingleCharToken MULT = new SingleCharToken('*');
    public static SingleCharToken DIVIDE = new SingleCharToken('/');
    public static SingleCharToken LPAREN = new SingleCharToken('(');
    public static SingleCharToken RPAREN = new SingleCharToken(')');
    public static SingleCharToken ASSIGN = new SingleCharToken('=');
    public static SingleCharToken AND = new SingleCharToken('&');
    public static SingleCharToken OR = new SingleCharToken('|');
    public static SingleCharToken NOT = new SingleCharToken('!');
    public static SingleCharToken DOT = new SingleCharToken('.');

    internal static readonly SingleCharToken[] Values = { PLUS, MINUS, MULT, DIVIDE, LPAREN, RPAREN, ASSIGN, AND, OR, NOT, DOT };

    public readonly char c;
    
    private SingleCharToken(char c)
    {
        this.c = c;
    }
    
    public static SingleCharToken get(char c)
    {
        foreach (var value in Values)
        {
            if (value.c == c)
            {
                return value;
            }
        }
        return null;
    }

    public override string ToString()
    {
        return "" + c;
    }

    public override string GetPlaceholder()
    {
        return "" + c;
    }
}

public class MultiCharToken : Token
{
    public static MultiCharToken EQUALS = new MultiCharToken(SingleCharToken.ASSIGN, SingleCharToken.ASSIGN);
    public static MultiCharToken NOT_EQUALS = new MultiCharToken(SingleCharToken.NOT, SingleCharToken.ASSIGN);
    public static MultiCharToken LOGICAL_AND = new MultiCharToken(SingleCharToken.AND, SingleCharToken.AND);
    public static MultiCharToken LOGICAL_OR = new MultiCharToken(SingleCharToken.OR, SingleCharToken.OR);
    
    internal static readonly MultiCharToken[] Values = { EQUALS, NOT_EQUALS, LOGICAL_AND, LOGICAL_OR };
    
    private readonly SingleCharToken[] tokens;

    private MultiCharToken(params SingleCharToken[] tokens)
    {
        this.tokens = tokens;
    }

    public static void ReplaceTokens(ParseResult parseResult)
    {
        // create a text representation of this token array so we can easy find and replace tokens with MultiCharTokens using string replace
        string tokenString = "";
        foreach (Token token in parseResult.Elements) // at this point every matchable is a token
        {
            string tokenStringRepresentation = token.ToString();
            tokenString += token.ToString();
        }

        foreach (MultiCharToken multiCharToken in Values)
        {
            string id = multiCharToken.ToString();
            int index = tokenString.IndexOf(id, StringComparison.Ordinal);
            while (index != -1)
            {
                parseResult.ReplaceRange(index, id.Length, multiCharToken);
                index = tokenString.IndexOf(id, index + id.Length, StringComparison.Ordinal);
            }
        }
    }

    public override string ToString()
    {
        string text = "";
        foreach (SingleCharToken token in tokens) text += token.ToString();
        return text;
    }

    public override string GetPlaceholder()
    {
        return ToString();
    }
}