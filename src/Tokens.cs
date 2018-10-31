namespace Compiler {

public enum TokenType {Identifier}

public abstract class Token {
    public TokenType Type { get; protected set; }
    public int Column { get; protected set; }
    public int Line { get; protected set; }
    public abstract string GetStringValue();
}

public class IdentityToken : Token {
    private readonly string _value;

    public IdentityToken(string value, int line, int col) {
        _value = value;
        Line = line;
        Column = col;
        Type = TokenType.Identifier;
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

}