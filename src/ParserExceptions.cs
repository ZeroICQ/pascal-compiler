namespace Compiler {

public abstract class ParserException : ParsingException {
    protected ParserException(string lexeme, int line, int column) : base(lexeme, line, column) {}
};

public class IllegalExprException : ParserException {
    public override string Message => $"Illegal expression {Lexeme} at {Line},{Column}";
    public IllegalExprException(string lexeme, int line, int column) : base(lexeme, line, column) { }
}

}