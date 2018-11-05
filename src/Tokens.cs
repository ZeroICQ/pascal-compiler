using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace Compiler {

public enum TokenType {Identifier, Integer, Real, String, Eof, Operator, Separator}

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

public class IdentityToken : Token {
    public override TokenType Type => TokenType.Identifier;
    public override string Lexeme { get; }
    
    public override string StringValue => Lexeme.ToLower();

    public IdentityToken(string lexeme, int line, int column) : base(line, column) {
        Lexeme = lexeme;
    }
}

public abstract class NumberToken : Token {
    protected NumberToken(int line, int column) : base(line, column) {}
}

public class IntegerToken : NumberToken {
    public override TokenType Type => TokenType.Integer;
    public override string Lexeme { get; }
    public override string StringValue => _value.ToString();
    
    private readonly long _value;

    public IntegerToken(string lexeme, int line, int column) : base(line, column) {
        Lexeme = lexeme;
        //hex
        if (lexeme.StartsWith('$')) 
            _value = long.Parse(lexeme.Substring(1), NumberStyles.HexNumber);
        //oct
        else if (lexeme.StartsWith('&'))
            _value = Convert.ToInt64(lexeme.Substring(1), 8);
        //bin
        else if (lexeme.StartsWith('%'))
            _value = Convert.ToInt64(lexeme.Substring(1), 2);
        //dec
        else
            _value = long.Parse(lexeme);
    }
}

public class RealToken : NumberToken {
    public override TokenType Type => TokenType.Real;
    public override string Lexeme { get; }
    public override string StringValue => _value.ToString(CultureInfo.InvariantCulture);
    
    private readonly double _value;
    static readonly NumberFormatInfo NumberFormat = new NumberFormatInfo {NumberDecimalSeparator = "."};

    public RealToken(string lexeme, int line, int column) : base(line, column) {
        Lexeme = lexeme;
        _value = double.Parse(lexeme, NumberStyles.Float, NumberFormat);
    }
}

public class StringToken : Token {
    private enum States {AfterHash, Dec, Hex, Oct, Bin, StringStart, QuotedString, AfterQMark} 
        
    public override TokenType Type => TokenType.String;
    public override string Lexeme { get; }
    public override string StringValue => _value;

    private string _value;
    
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
                    if (Symbols.decDigits.Contains((char) symbol)) {
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
                    if (Symbols.hexDigits.Contains((char) symbol)) {
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
                    if (Symbols.octDigits.Contains((char) symbol)) {
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
        
        _value = value.ToString();
    }
}

public class OperatorToken : Token {
    public override TokenType Type => TokenType.Operator;
    public override string Lexeme { get; }
    public override string StringValue => _operation.ToString();

    public enum Operation {
        Plus,                   // +
        Minus,                  // -
        Multiply,               // *
        Divide,                 // /
        Equal,                  // =
        Less,                   // <
        More,                   // >
        OpenBracket,            // [
        CloseBracket,           // ]
        Dot,                    // .
        OpenParenthesis,        // (
        CloseParenthesis,       // )    
        Caret,                  // ^
        AtSign,                 // @
        NotEqual,               // <>
        BitwiseShiftLeft,       // <<
        BitwiseShiftRight,      // >>
        Exponential,            // **
        SymmetricDifference,    // ><
        LessOrEqual,            // <=
        MoreOreEqual,           // >=
        Assign,                 // :=
        PlusAssign,             // +=
        MinusAssign,            // -=
        MultiplyAssign,         // *=
        DivideAssign,           // /=
        OpenParenthesisWithDot, // (.
        CloseParenthesisWithDot // .)
    }

    private Operation _operation;

    public OperatorToken(string lexeme, Operation op, int line, int column) : base(line, column) {
        Lexeme = lexeme;
        _operation = op;
    }
}

public class SeparatorToken : Token {
    public override TokenType Type => TokenType.Separator;
    public override string Lexeme { get; }
    public override string StringValue => _val.ToString();

    public enum Separator {
        Comma,     // ,
        Colon,     // :
        Semicolon  // ; 
    }

    private Separator _val;

    public SeparatorToken(string lexeme, Separator sep, int line, int column) : base(line, column) {
        Lexeme = lexeme;
        _val = sep;
    }
}

}
