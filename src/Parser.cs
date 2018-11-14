using System.Linq.Expressions;

namespace Compiler {
public class Parser {
    private LexemesAutomata _lexer;

    public Parser(LexemesAutomata lexer) {
        _lexer = lexer;
    }
    
    public AstNode GetAst() {
        var root = new BlockNode();
        
        while (!(_lexer.GetNextToken() is EofToken)) {
            _lexer.Retract();
            root.AddExpression(ParseExpression(0));
        }
        return root;
    }

    private ExprNode ParseExpression(int priority) {
        if (priority >= _operators.Length) {
            return ParseFactor();
        }
        
        var node = ParseExpression(priority + 1);

        while (true) {
            var op = _lexer.GetNextToken();
            if (!_operators[priority].IsBelong(op)) {
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
            case IntegerToken integerToken:
                return new IntegerNode(integerToken);
            default:
                Illegal(t);
                break;
        }

        return null;
    }
    
//    private AstNode ParseSimpleExpr() {
//        var node = ParseTerm();
//
//        while (true) {
//            var token = _lexer.GetNextToken();
//            switch (token) {
//                    case OperatorToken operatorToken:
//                        switch (operatorToken.Value) {
//                                case Symbols.Operators.Plus:
//                                case Symbols.Operators.Minus:
//                                    node = BuildNode(operatorToken, node, null);
//                                    break;
//                        }
//                        break;
//            }
//            
//            node.Ri
//        }
//        
//        return node;
//    }

    // Checks

    // Require
    
    // Factories
//    private static AstNode BuildNode(Token token, params AstNode[] p) {
//        switch (token) {
//                case OperatorToken operatorToken:
//                    switch (operatorToken.Value) {
//                            case Symbols.Operators.More:
//                                return new MoreNode(p[0], p[1]);
//                            case Symbols.Operators.MoreOreEqual:
//                                return new MoreNode(p[0], p[1]);
//                    }
//
//        }
//    }

    private void Require(Symbols.Operators op)  {
        var t = _lexer.GetNextToken();
        if (!(t is OperatorToken _op && _op.Value == op)) {
            Illegal(t);
        }
    }

    private void Illegal(Token token) {
        throw new IllegalExprException(token.Lexeme, token.Line, token.Column);
    }
    
    //priorities 
    private static readonly Operator[] _operators;

    static Parser() {
        _operators = new Operator[] {
            new TermOperator(), 
            new FactorOperator(), 
        };
    }
    
}


public abstract class Operator {
    public abstract bool IsBelong(Token token);
}

public class TermOperator : Operator {
    public override bool IsBelong(Token token) {
        switch (token) {
                case OperatorToken opToken:
                    switch (opToken.Value) {
                        case Symbols.Operators.Plus:
                        case Symbols.Operators.Minus:
                            return true;
                        default:
                            return false;
                    }
                case ReservedToken reservedToken:
                    switch (reservedToken.Value) {
                        case Symbols.Words.Or:
                        case Symbols.Words.Xor:
                            return true;
                        default:
                            return false;
                    }
            default:
                    return false;
        }
    }
}

public class FactorOperator : Operator {
    public override bool IsBelong(Token token) {
        switch (token) {
            case OperatorToken operatorToken:
                switch (operatorToken.Value) {
                    case Symbols.Operators.Multiply:
                    case Symbols.Operators.Divide:
                        return true;
                    default:
                        return false;
                        
                }
            case ReservedToken reservedToken:
                switch (reservedToken.Value) {
                        case Symbols.Words.Div:
                        case Symbols.Words.Mod:
                        case Symbols.Words.And:
                        case Symbols.Words.Shl:
                        case Symbols.Words.Shr:
                            return true;
                        default:
                            return false;
                }
            default:
                return false;
        }
    }
}

}
