namespace Compiler {

public enum TokenType {Identifier}

public abstract class Token {
    public TokenType Type { get; protected set; }
    public int Column { get; protected set; }
    public int Line { get; protected set; }
    public abstract string GetStringValue();
}

public class EofToken : Token {
    public override string GetStringValue() {
        return "EOF";
    }
}

public class IdentityToken : Token {
    private readonly string _value;

    public IdentityToken(string value, int line, int col) {
        _value = value;
        Type = TokenType.Identifier;
        Line = line;
        Column = col;
    }

    public override string GetStringValue() {
        return _value.ToLower();
    }

    public override string ToString() {
        return _value;
    }
}

//public class OperatorToken : Token {
//    public enum Type {Plus, Minus}
//    
//    public Type Value { get; }
//
//    public override string ToString() {
//        return "Some Operator";
//    }
//}

//Numbers
public abstract class Number : Token {}

public abstract class Integer: Number {}

public abstract class SignedInteger: Integer {
    protected int value;
}

public abstract class UnsignedInteger : Integer {
    public uint Value {get; protected set; }

    public UnsignedInteger(uint val, int line, int column) {
        Line = line;
        Column = column;
        Value = val;
    }
}

}