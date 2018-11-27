using System.Collections.Generic;

namespace Compiler {
public class Parser {
    private LexemesAutomata _lexer;

    public Parser(LexemesAutomata lexer) {
        _lexer = lexer;
    }
    
    public AstNode Parse() {
        var mainBlock = ParseCompoundStatement();
        Require(Symbols.Operators.Dot);
        mainBlock.IsMain = true;
        return mainBlock;
    }
    
    // retracts
    private BlockNode ParseCompoundStatement() {
        var compoundStatement = new BlockNode();
        
        Require(Symbols.Words.Begin);

        while (!Check(_lexer.GetNextToken(), Symbols.Words.End)) {
            _lexer.Retract();
            compoundStatement.AddStatement(ParseStatement());
            Require(Symbols.Separators.Semicolon);
        }
        
        _lexer.Retract();
        Require(Symbols.Words.End);
        return compoundStatement;
    }

    private StatementNode ParseStatement() {
        //TODO: parse simple statements: assignment, procedure; Structured
        var t = _lexer.GetNextToken();
                
        switch (t) {
            case IdentifierToken identifier:
                _lexer.Retract();
                var left = ParseExpression(0);
                
                // now it can be either procedure statement or assign
                var nextToken = _lexer.GetNextToken();

                switch (nextToken) {
                    case OperatorToken op:
                        switch (op.Value) {
                            case Symbols.Operators.Assign:
                            case Symbols.Operators.PlusAssign:
                            case Symbols.Operators.MinusAssign:
                            case  Symbols.Operators.MultiplyAssign:
                            case  Symbols.Operators.DivideAssign:
                                return new AssignNode(left, op, ParseExpression(0));
                            
                            case Symbols.Operators.OpenParenthesis:
                                //function call
                                _lexer.Retract();
                                var paramList = ParseParamList();
                                return new ProcedureCallNode(left, paramList);
                                
                        }
                        break;
                }
                break;
                
            // compound
            case ReservedToken reserved:
                switch (reserved.Value) {
                    case Symbols.Words.Begin:
                        _lexer.Retract();
                        return ParseCompoundStatement();
                }
                break;
                
        }

        throw Illegal(t);
    }
    
    private enum ParseParamListStates {Start, AfterFirst}
    // parse (expr {, expr})
    private List<ExprNode> ParseParamList() {
        var parameters = new List<ExprNode>();
        Require(Symbols.Operators.OpenParenthesis);

        var state = ParseParamListStates.Start;

        while (true) {
            var t = _lexer.GetNextToken();
            
            switch (state) {
                case ParseParamListStates.Start:
                    if (Check(t, Symbols.Operators.CloseParenthesis))
                        return parameters;
                    _lexer.Retract();
                    
                    state = ParseParamListStates.AfterFirst;
                    parameters.Add(ParseExpression(0));
                    break;
                
                case ParseParamListStates.AfterFirst:
                    if (Check(t, Symbols.Operators.CloseParenthesis))
                        return parameters;
                    _lexer.Retract();
                    
                    Require(Symbols.Separators.Comma);
                    parameters.Add(ParseExpression(0));
                    break;
            }
            
        }
    }

    
//    private enum AssignNodeStates {Start, AfterDot, AfterBracket}
//    // id{.id}{[expression]}, 
//    public VariableReferenceNode ParseVariableReference() {
//        
//        VariableReferenceNode varRef;
//        
//        //get first
//        Token t;
//        switch (t = _lexer.GetNextToken()) {
//            case IdentifierToken identifier:
//                varRef = new IdentifierNode(identifier);
//                break;
//            default:
//                _lexer.Retract();
//                throw Illegal(t);
//        }
//
//        var state = AssignNodeStates.Start;
//
//        //parse variable part
//        while (true) {
//            t = _lexer.GetNextToken();
//            
//            switch (state) {
//                
//                case AssignNodeStates.Start:
//
//                    if (Check(t, Symbols.Operators.Dot))
//                        state = AssignNodeStates.AfterDot;
//                    else if (Check(t, Symbols.Operators.OpenBracket))
//                        state = AssignNodeStates.AfterBracket;
//                    else {
//                        _lexer.Retract();
//                        return varRef;
//                    } 
//                    break;
//                
//                case AssignNodeStates.AfterDot:
//
//                    switch (t) {
//                        case IdentifierToken identifierToken:
//                            state = AssignNodeStates.Start;
//                            varRef = new AccessNode(varRef, identifierToken);
//                            break;
//                        default:
//                            throw Illegal(t);
//                    }
//                    break;
//                    
//                case AssignNodeStates.AfterBracket:
//                    var expr = ParseExpression(0);
//                    Require(Symbols.Operators.CloseBracket);
//                    varRef = new IndexNode(varRef, expr);
//                    state = AssignNodeStates.Start;
//                    break;
//            }
//        }
//    }
    

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

        if (!Check(t, op)) {
            _lexer.Retract();
            throw new IllegalExprException(t.Lexeme, t.Line, t.Column, op.ToString());            
        }
        
    }

    private void Require(Symbols.Words word) {
        var t = _lexer.GetNextToken();
        
        if (!Check(t, word)) {
            _lexer.Retract();
            throw new IllegalExprException(t.Lexeme, t.Line, t.Column, word.ToString());
        }
    }

    private void Require(Symbols.Separators sep) {
        var t = _lexer.GetNextToken();
        if (!Check(t, sep)) {
            throw new IllegalExprException(t.Lexeme, t.Line, t.Column, sep.ToString());
        }
    }

    private void Require<T>() where T : TokenGroup, new() {
        var t = _lexer.GetNextToken();
        if (!Check<T>(t)) {
            _lexer.Retract();
            throw Illegal(t);
        }
        
    }
    
    private ParserException Illegal(Token token) {
        return new IllegalExprException(token.Lexeme, token.Line, token.Column);
    }

    private bool Check(Token t, Symbols.Operators op) {
        return t is OperatorToken _op && _op.Value == op;
        
    }

    private bool Check(Token t, Symbols.Words word) {
        return t is ReservedToken _w && _w.Value == word;
    }
    
    private bool Check(Token t, Symbols.Separators sep) {
        return t is SeparatorToken _s && _s.Value == sep;
    }

    private bool Check<T>(Token t) where T : TokenGroup, new() {
        var tokenGroup = new T();
        return tokenGroup.Contains(t);
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