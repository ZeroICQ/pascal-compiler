using System;

namespace Compiler {

public enum TokenType {Identifier, Integer, Eof}

public abstract class Token {
    public TokenType Type { get; protected set; }
    public int Column { get; }
    public int Line { get; }
    public abstract string Lexeme { get; }
    public abstract string StringValue { get; }

    protected Token(int line, int column) {
        Line = line;
        Column = column;
    }
}

public class EofToken : Token {
    public override string StringValue => "EOF";
    public override string Lexeme => String.Empty;

    public EofToken(int line, int column) : base(line, column) {
        Type = TokenType.Eof;
    }
}

public class IdentityToken : Token {
    private readonly string _value;
    public override string StringValue => _value.ToLower();
    public override string Lexeme => _value;

    public IdentityToken(string value, int line, int column) : base(line, column) {
        Type = TokenType.Identifier;
        _value = value;
    }
}

public abstract class NumberToken : Token {
    protected NumberToken(int line, int column) : base(line, column) {}
}

public abstract class IntegerToken : NumberToken {
    private readonly int _value;

    public IntegerToken(int value, int line, int column) : base(line, column) {
        Type = TokenType.Integer;
        _value = value;
    }
}
}
