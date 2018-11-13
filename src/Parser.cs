namespace Compiler {
public class Parser {
    private LexemesAutomata _lexer;

    public Parser(LexemesAutomata lexer) {
        _lexer = lexer;
    }
    
    public AstNode GetAst() {
        var root = new RootNode();
        while (!(_lexer.GetNextToken() is EofToken)) {
            _lexer.Retract();
            root.AppendChild(ParseFactor());

        }
        return root;
    }

//    private AstNode ParseSimpleExpr() {
//        whi
//    }
//
//    private AstNode ParseOperator() {
//        
//    }

    private AstNode ParseFactor() {
        var t = _lexer.GetNextToken();
        switch (t) {
                case IntegerToken integerToken:
                    return new IntegerNode(integerToken.Value);
        }
        return new IntegerNode(0);
    }
    
    // Requires

    private void RequireRelational() {
        
    }
}
}
