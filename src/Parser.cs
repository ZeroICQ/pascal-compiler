namespace Compiler {
public class Parser {
    private LexemesAutomata _lexer;

    public Parser(LexemesAutomata lexer) {
        _lexer = lexer;
    }
    
    public AstNode Parse() {
        var program = new BlockNode();

        if (!(t is ReservedToken reservedToken && reservedToken.Value == Symbols.Words.Begin)) {
            
        }
        
        
        while (!(_lexer.GetNextToken() is EofToken)) {
            _lexer.Retract();
            root.AddExpression(ParseExpression(0));
        }
        return root;
    }

    private ExprNode ParseExpression(int priority) {
        if (priority >= TokenPriorities.Length) {
            return ParseFactor();
        }
        
        var node = ParseExpression(priority + 1);

        while (true) {
            var op = _lexer.GetNextToken();
            if (!TokenPriorities[priority].Contains(op)) {
                _lexer.Retract();
                break;
            }
            
            var right = ParseExpression(priority + 1);;
            node = new BinaryExprNode(node, right, op);
        }

        return node;
    }

    private ExprNode ParseFactor() {
        var t = _lexer.GetNextToken();
        switch (t) {
            case OperatorToken operatorToken:
                switch (operatorToken.Value) {
                    case Symbols.Operators.OpenParenthesis:
                        var exp = ParseExpression(0);
                        Require(Symbols.Operators.CloseParenthesis);
                        return exp;
                }

                break;
            case FloatToken floatToken:
                return new FloatNode(floatToken);
            case IntegerToken integerToken:
                return new IntegerNode(integerToken);
            case IdentifierToken identityToken:
                return new IdentifierNode(identityToken);
            default:
                throw Illegal(t);
        }

        throw Illegal(t);
    }

    //priorities 

    private static readonly TokenGroup[] TokenPriorities;

    static Parser() {
        TokenPriorities = new TokenGroup[] {
            new TermTokenGroup(), 
            new FactorTokenGroup(), 
        };
    }

    private void Require(Symbols.Operators op)  {
        var t = _lexer.GetNextToken();
        
        if (!(t is OperatorToken _op && _op.Value == op)) {
            _lexer.Retract();
            throw new IllegalExprException(t.Lexeme, t.Line, t.Column, op.ToString());
        }
    }

    private void Require(Symbols.Words word) {
        var t = _lexer.GetNextToken();
        
        if (!(t is ReservedToken _w && _w.Value == word)) {
            _lexer.Retract();
            throw new IllegalExprException(t.Lexeme, t.Line, t.Column, word.ToString());
        }
    }

    private void Require<T>() where T : TokenGroup, new() {
        var t = _lexer.GetNextToken();
        var tokenGroup = new T();
        tokenGroup.Contains(t);
    }

    private ParserException Illegal(Token token) {
        return new IllegalExprException(token.Lexeme, token.Line, token.Column);
    }
}


public abstract class TokenGroup {
    public abstract bool Contains(Token token);
}

public class TermTokenGroup : TokenGroup {
    public override bool Contains(Token token) {
        switch (token) {
            case OperatorToken opToken:
                switch (opToken.Value) {
                    case Symbols.Operators.Plus:
                    case Symbols.Operators.Minus:
                        return true;
                }
                break;
            
            case ReservedToken reservedToken:
                switch (reservedToken.Value) {
                    case Symbols.Words.Or:
                    case Symbols.Words.Xor:
                        return true;
                }
                break;
        }
        
        return false;
    }
}

public class FactorTokenGroup : TokenGroup {
    public override bool Contains(Token token) {
        switch (token) {
            case OperatorToken operatorToken:
                switch (operatorToken.Value) {
                    case Symbols.Operators.Multiply:
                    case Symbols.Operators.Divide:
                        return true;
                }
                break;
            
            case ReservedToken reservedToken:
                switch (reservedToken.Value) {
                    case Symbols.Words.Div:
                    case Symbols.Words.Mod:
                    case Symbols.Words.And:
                    case Symbols.Words.Shl:
                    case Symbols.Words.Shr:
                        return true;
                }
                break;
        }

        return false;
    }
}

}
