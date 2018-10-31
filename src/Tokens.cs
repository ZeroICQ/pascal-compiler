namespace Compiler {

public abstract class Token {
    public int Column { get; protected set; }
    public int Line { get; protected set; }
    
}

public class IdentityToken : Token {
    private readonly string _value;

    public IdentityToken(string value, int line, int col) {
        _value = value;
        Line = line;
        Column = col;
    }
    
    public override string ToString() {
        return _value;
    }
}

public class OperatorToken : Token {
    public enum Type {Plus, Minus}
    
    public Type Value { get; }

    public override string ToString() {
        return "Some Operator";
    }
}

}