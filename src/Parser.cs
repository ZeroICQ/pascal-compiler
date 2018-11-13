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
            root.AppendChild(ParseExpression());
        }
        return root;
    }

    private AstNode ParseExpression() {
        var node = ParseSimpleExpr();

        while (true) {
            var op = _lexer.GetNextToken();
            if (!IsRelational(op)) {
                _lexer.Retract();
                break;
            }
            var right = ParseSimpleExpr();
            node = BuildNode(op, node, right);
        }

        return node;
    }
    
    private AstNode ParseSimpleExpr() {
        var node = ParseTerm();

        while (true) {
            var token = _lexer.GetNextToken();
            switch (token) {
                    case OperatorToken operatorToken:
                        switch (operatorToken.Value) {
                                case Symbols.Operators.Plus:
                                case Symbols.Operators.Minus:
                                    node = BuildNode(operatorToken, node, null);
                                    break;
                        }
                        break;
            }
            
            node.Ri
        }
        
        return node;
    }

    private AstNode ParseTerm() {
        
    }

    private AstNode ParseFactor() {
        var t = _lexer.GetNextToken();
        switch (t) {
                case IntegerToken integerToken:
                    return new IntegerNode(integerToken.Value);
        }
        return new IntegerNode(0);
    }
    
    // Checks

    private bool IsRelational(Token token) {
        
    }
    
    // Require
    
    // Factories
    private static AstNode BuildNode(Token token, params AstNode[] p) {
        switch (token) {
                case OperatorToken operatorToken:
                    switch (operatorToken.Value) {
                            case Symbols.Operators.More:
                                return new MoreNode(p[0], p[1]);
                            case Symbols.Operators.MoreOreEqual:
                                return new MoreNode(p[0], p[1]);
                    }

        }
    }
}
}
