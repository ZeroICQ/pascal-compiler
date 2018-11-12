namespace Compiler {
public class Parser {
    private LexemesAutomata _lexer;

    public Parser(LexemesAutomata lexer) {
        _lexer = lexer;
    }
    
    public AstNode GetAst() {
        var root = new RootNode();
        root.AppendChild(ParseFactor());        
        return root;
    }

    private AstNode ParseFactor() {
        var t = _lexer.GetNextToken();
        switch (t.Type) {
                case TokenType.Integer:
                    return new NumberNode();
        }
    }
}
}