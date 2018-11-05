using System;
using System.Globalization;

namespace Compiler {

public enum TokenType {Identifier, Integer, Real, Eof}

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
            _value = int.Parse(lexeme.Substring(1), NumberStyles.HexNumber);
        //dec
        else
            _value = int.Parse(lexeme);
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

}
