using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace Compiler {

public enum TokenType {Identifier, Integer, Real, String, Eof, Operator, Separator, Reserved}

public abstract class Token {
    public int Column { get; }
    public int Line { get; }
    
    public abstract TokenType Type { get; }
    public abstract string Lexeme { get; }
    public abstract string StringValue { get; }

    protected Token(int line, int column) {
        Line = line;
        Column = column;
    }
}

public class EofToken : Token {
    public override TokenType Type => TokenType.Eof;
    public override string Lexeme => String.Empty;
    public override string StringValue => "EOF";

    public EofToken(int line, int column) : base(line, column) {
    }
}

public class IdentifierToken : Token {
    public override TokenType Type => TokenType.Identifier;
    public override string Lexeme { get; }

    public override string StringValue => Value;
    public string Value { get; }
    
    public IdentifierToken(string lexeme, int line, int column) : base(line, column) {
        Lexeme = lexeme;
        if (lexeme.StartsWith('&'))
            Value = lexeme.Substring(1).ToLower();
        else
            Value = lexeme.ToLower();
    }
}

public abstract class ConstantToken : Token {
    protected ConstantToken(int line, int column) : base(line, column) {}
}

public abstract class NumberToken : ConstantToken {
    protected NumberToken(int line, int column) : base(line, column) {}
}

public class IntegerToken : NumberToken {
    public override TokenType Type => TokenType.Integer;
    public override string Lexeme { get; }
    public override string StringValue => Value.ToString();
    
    public long Value { get; }

    public IntegerToken(string lexeme, int line, int column) : base(line, column) {
        Lexeme = lexeme;

        try {
            //hex
            if (lexeme.StartsWith('$')) 
                Value = long.Parse(lexeme.Substring(1), NumberStyles.HexNumber);
            //oct
            else if (lexeme.StartsWith('&'))
                Value = Convert.ToInt64(lexeme.Substring(1), 8);
            //bin
            else if (lexeme.StartsWith('%'))
                Value = Convert.ToInt64(lexeme.Substring(1), 2);
            //dec
            else
                Value = long.Parse(lexeme);
        }
        catch (OverflowException) {
            throw new IntegerLiteralOverflowException(Lexeme, Line, Column);
        }
    }
}

public class FloatToken : NumberToken {
    public override TokenType Type => TokenType.Real;
    public override string Lexeme { get; }
    public override string StringValue => Value.ToString(CultureInfo.InvariantCulture);

    public double Value { get; }
    static readonly NumberFormatInfo NumberFormat = new NumberFormatInfo {NumberDecimalSeparator = "."};

    public FloatToken(string lexeme, int line, int column) : base(line, column) {
        Lexeme = lexeme;
        try {
            Value = double.Parse(lexeme, NumberStyles.Float, NumberFormat);
        }
        catch (OverflowException) {
            throw new RealLiteralOverflowException(lexeme, Line, Column);
        }
    }
}

public class StringToken : ConstantToken {
    private enum States {AfterHash, Dec, Hex, Oct, Bin, StringStart, QuotedString, AfterQMark} 
        
    public override TokenType Type => TokenType.String;
    public override string Lexeme { get; }
    public string Value { get; private set; }
    public override string StringValue => Value;

    
    public StringToken(string lexeme, int line, int column) : base(line, column) {
        Lexeme = lexeme;
        ParseValue();
    }

    private void ParseValue() {
        var value = new StringBuilder() {Capacity = Lexeme.Length};

        //ASK: do better?
        var controlSeq = new StringBuilder();
        var currState = States.StringStart;

        MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(Lexeme));
        
        var input = new StreamReader(stream);
        
        bool process = true;
        while (process) {
            var symbol = input.Read();
            try {
                switch (currState) {
                
                case States.AfterHash:
                    controlSeq.Clear();
                    
                    if (symbol == '$')
                        currState = States.Hex;
                    else if (symbol == '&')
                        currState = States.Oct;
                    else if (symbol == '%')
                        currState = States.Bin;
                    else {
                        controlSeq.Append((char) symbol);
                        currState = States.Dec;
                    }
                    break;
                case States.Dec:
                    if (Constants.decDigits.Contains((char) symbol)) {
                        controlSeq.Append((char) symbol);
                    } 
                    else {
                        value.Append((char) int.Parse(controlSeq.ToString()));
                        if (symbol == '#')
                            currState = States.AfterHash;
                        else if (symbol == '\'')
                            currState = States.QuotedString;
                        else
                            currState = States.StringStart;
                                
                        if (symbol != -1 && symbol != '#' && symbol != '\'')
                            value.Append((char) symbol);
                    }
                    break;
                case States.Hex:
                    if (Constants.hexDigits.Contains((char) symbol)) {
                        controlSeq.Append((char) symbol);
                    }
                    else {
                        value.Append((char) int.Parse(controlSeq.ToString(), NumberStyles.HexNumber));
                        if (symbol == '#')
                            currState = States.AfterHash;
                        else if (symbol == '\'')
                            currState = States.QuotedString;
                        else
                            currState = States.StringStart;

                        if (symbol != -1 && symbol != '#' && symbol != '\'')
                            value.Append((char) symbol);
                    }
                    break;
                case States.Oct:
                    if (Constants.octDigits.Contains((char) symbol)) {
                        controlSeq.Append((char) symbol);
                    }
                    else {
                        value.Append((char) Convert.ToInt64(controlSeq.ToString(), 8));
                        if (symbol == '#')
                            currState = States.AfterHash;
                        else if (symbol == '\'')
                            currState = States.QuotedString;
                        else
                            currState = States.StringStart;

                        if (symbol != -1 && symbol != '#' && symbol != '\'')
                            value.Append((char) symbol);
                    }
                    break;
                case States.Bin:
                    if (symbol == '0' || symbol == '1') {
                        controlSeq.Append((char) symbol);
                    }
                    else  {
                        value.Append((char) Convert.ToInt64(controlSeq.ToString(), 2));
                        if (symbol == '#')
                            currState = States.AfterHash;
                        else if (symbol == '\'')
                            currState = States.QuotedString;
                        else
                            currState = States.StringStart;

                        if (symbol != -1 && symbol != '#' && symbol != '\'')
                            value.Append((char) symbol);
                    }
                    break;
                case States.StringStart:
                    // !automata must guarantee this invariant
                    if (symbol == '#') {
                        currState = States.AfterHash;
                    }
                    else if (symbol == '\'') {
                        currState = States.QuotedString;
                    }
                    else if (symbol == -1)
                        process = false;
                    break;
                case States.QuotedString:
                    if (symbol == '\'')
                        currState = States.AfterQMark;
                    else {
                        value.Append((char) symbol);
                    }
                    break;
                
                case States.AfterQMark:
                    if (symbol == '\'') {
                        value.Append('\'');
                        currState = States.QuotedString;
                    }
                    else if (symbol == '#')
                        currState = States.AfterHash;
                    else if (symbol == -1)
                        process = false;
                    break;
                }
            }
            catch (OverflowException) {
                throw new IntegerLiteralOverflowException(Lexeme, Line, Column);
            }
        }
        
        Value = value.ToString();
    }
}

public class OperatorToken : Token {
    public override TokenType Type => TokenType.Operator;
    public override string Lexeme { get; }
    public override string StringValue => Value.ToString();
    
    public Constants.Operators Value { get; }

    public OperatorToken(string lexeme, Constants.Operators op, int line, int column) : base(line, column) {
        Lexeme = lexeme;
        Value = op;
    }
}

public class ReservedToken : Token {
    public override TokenType Type => TokenType.Reserved;
    public override string Lexeme { get; }
    public override string StringValue => Value.ToString();

    public Constants.Words Value { get; }

    public ReservedToken(string lexeme, Constants.Words word, int line, int column) : base(line, column) {
        Lexeme = lexeme;
        Value = word;
    }
}

public class SeparatorToken : Token {
    public override TokenType Type => TokenType.Separator;
    public override string Lexeme { get; }
    public override string StringValue => Value.ToString();

    public Constants.Separators Value { get; }

    public SeparatorToken(string lexeme, Constants.Separators sep, int line, int column) : base(line, column) {
        Lexeme = lexeme;
        Value = sep;
    }
}

}
