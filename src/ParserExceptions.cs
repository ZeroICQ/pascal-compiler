namespace Compiler {

public abstract class ParserException : ParsingException {
    protected ParserException(string lexeme, int line, int column) : base(lexeme, line, column) {}
};

public class IllegalExprException : ParserException {
    public override string Message { get; } 

    public IllegalExprException(string lexeme, int line, int column) : base(lexeme, line, column) {
        Message = $"Illegal expression {Lexeme} at {Line},{Column}";
    }
    
    public IllegalExprException(string lexeme, int line, int column, string expected) : base(lexeme, line, column) {
        Message = $"Illegal expression {Lexeme} at {Line},{Column}. Expected {expected}";
    }
    
}

public class NotAllowedException : ParserException {
    public override string Message => $"{Lexeme} not allowed at {Line},{Column}.";
    public NotAllowedException(string lexeme, int line, int column) : base(lexeme, line, column) { }
}

}