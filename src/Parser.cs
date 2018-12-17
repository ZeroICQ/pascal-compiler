using System.Collections.Generic;

namespace Compiler {
public class Parser {
    private LexemesAutomata _lexer;
    private ulong _cyclesCounter = 0;
    private SymStack _symStack;
    private bool _checkSemantics;
    private SemanticsVisitor _semanticsVisitor;

    public Parser(LexemesAutomata lexer, bool checkSemantics) {
        _lexer = lexer;
        _symStack = new SymStack();
        _checkSemantics = checkSemantics;
        _semanticsVisitor = new SemanticsVisitor(_symStack);
        
        // Global namespace
        _symStack.Push();
    }

    public AstNode Parse() { 
        ParseDeclarations();
        
        var mainBlock = ParseCompoundStatement();
        Require(Constants.Operators.Dot);
        mainBlock.IsMain = true;
        return mainBlock;
    }


    private void ParseDeclarations() {
        while (true) {
            var t = _lexer.GetNextToken();
            
            if (Check(t, Constants.Words.Var)) {
                ParseVariableDeclarations();
                continue;
            }
            break;
        }

        _lexer.Retract();
    }

    // start after "var"
    // todo: add arrays
    private enum ParseVariableDeclarationsStates {Start, SingleVariable, MultipleVariables} 
    private void ParseVariableDeclarations() {
        var state = ParseVariableDeclarationsStates.Start;
        
        var identifiers = new List<IdentifierToken>();

        bool isFirst = true;
        while (true) {
            var t = _lexer.GetNextToken();
            switch (state) {
                case ParseVariableDeclarationsStates.Start:
                    identifiers.Clear();
                    if (t is IdentifierToken identifierToken)
                        
                        identifiers.Add(identifierToken);
                    else {
                        _lexer.Retract();
                        if (isFirst)
                            throw Illegal(t);
                        return;
                    }
                        

                    var next = _lexer.GetNextToken();
                    if (Check(next, Constants.Separators.Comma))
                        state = ParseVariableDeclarationsStates.MultipleVariables;
                    else if (Check(next, Constants.Separators.Colon))
                        state = ParseVariableDeclarationsStates.SingleVariable;
                    else {
                        _lexer.Retract();
                        throw Illegal(t);
                    }
                    break;
                //after "var identifier :"->[...]
                case ParseVariableDeclarationsStates.SingleVariable:
                    if (t is IdentifierToken typeToken) {
                        ExprNode initialExpr = null;
                        
                        if (Check(_lexer.GetNextToken(), Constants.Operators.Equal))
                            initialExpr = ParseExprWithCheck(true);                            
                        else
                            _lexer.Retract();    
                        
                        _symStack.AddVariable(identifiers[0], typeToken, initialExpr);
                        Require(Constants.Separators.Semicolon);
                        isFirst = false;
                        state = ParseVariableDeclarationsStates.Start;
                        break;
                    }
                    
                    _lexer.Retract();
                    throw Illegal(t);
                // starts after "var identifier,"->[...] 
                case ParseVariableDeclarationsStates.MultipleVariables:
                    if (t is IdentifierToken idToken) {
                        
                        identifiers.Add(idToken);

                        var nextToken = _lexer.GetNextToken();
                        if (Check(nextToken, Constants.Separators.Comma)) {
                            continue;
                        }
                        if (Check(nextToken, Constants.Separators.Colon)) {
                            // parse typeToken
                            var typeT = _lexer.GetNextToken();
                            if (typeT is IdentifierToken tpToken) {
                                foreach (var id in identifiers) {
                                    _symStack.AddVariable(id, tpToken);
                                }
                                Require(Constants.Separators.Semicolon);
                                isFirst = false;
                                state = ParseVariableDeclarationsStates.Start;
                                
                                break;
                            }
                        }
                    }
                    _lexer.Retract();
                    throw Illegal(t);
            }
        }
    }

    private StatementNode ParseStatementWithCheck() {
        var st = ParseStatement();
        if (_checkSemantics)
            st.Accept(_semanticsVisitor);
        return st;
    }
    
    // retracts
    private BlockNode ParseCompoundStatement() {
        var compoundStatement = new BlockNode();
        
        Require(Constants.Words.Begin);
        
        compoundStatement.AddStatement(ParseStatementWithCheck());
        while (true) {
            var hasSemicolon = Check(_lexer.GetNextToken(), Constants.Separators.Semicolon);
            
            if (!hasSemicolon) {
                _lexer.Retract();
                Require(Constants.Words.End);
                return compoundStatement;
            }

            if (Check(_lexer.GetNextToken(), Constants.Words.End))
                return compoundStatement;
            
            _lexer.Retract();
            compoundStatement.AddStatement(ParseStatementWithCheck());
        }
    }

    private StatementNode ParseStatement() {
        var t = _lexer.GetNextToken();
                
        switch (t) {
// todo: remove            
//            case IdentifierToken identifier:
//                _lexer.Retract();
//                var left = ParseExprWithCheck(false);
//
//                // todo: remove crunch?
//                if (left is FunctionCallNode f) {
//                    return new ProcedureCallNode(f.Name, f.Args);
//                }
//                
//                // now it can be either procedure statement or assign
//                var nextToken = _lexer.GetNextToken();
//
//                switch (nextToken) {
//                    case OperatorToken op:
//                        switch (op.Value) {
//                            case Constants.Operators.Assign:
//                            case Constants.Operators.PlusAssign:
//                            case Constants.Operators.MinusAssign:
//                            case  Constants.Operators.MultiplyAssign:
//                            case  Constants.Operators.DivideAssign:
//                                return new AssignNode(left, op, ParseExprWithCheck(false));
//                            
//                            case Constants.Operators.OpenParenthesis:
//                                //procedure call
//                                var paramList = ParseParamList();
//                                return new ProcedureCallNode(left, paramList);
//                        }
//                        break;
//                }
//                
//                _lexer.Retract();
//                throw Illegal(nextToken);
                
            // statements that starts with reserved words
            case ReservedToken reserved:
                switch (reserved.Value) {
                    case Constants.Words.Begin:
                        _lexer.Retract();
                        return ParseCompoundStatement();
                    
                    // if
                    case Constants.Words.If:
                        _lexer.Retract();
                        return ParseIfStatement();
                    // while
                    case Constants.Words.While:
                        _lexer.Retract();
                        return ParseWhileStatement(); 
                    case Constants.Words.For:
                        _lexer.Retract();
                        return ParseForStatement();
                    case Constants.Words.End:
                        _lexer.Retract();
                        return new EmptyStatementNode();
                    case Constants.Words.Continue:
                    case Constants.Words.Break:
                        if (_cyclesCounter == 0) {
                            throw new NotAllowedException(reserved.Lexeme, reserved.Line, reserved.Column);
                        }
                        return new ControlSequence(reserved);
                }
                break;
            
            // assignment
            default:
                _lexer.Retract();
                var left = ParseExprWithCheck(false);

                // todo: remove crunch?
                if (left is FunctionCallNode f) {
                    return new ProcedureCallNode(f.Name, f.Args);
                }
                
                // now it can be either procedure statement or assign
                var nextToken = _lexer.GetNextToken();

                switch (nextToken) {
                    case OperatorToken op:
                        switch (op.Value) {
                            case Constants.Operators.Assign:
                            case Constants.Operators.PlusAssign:
                            case Constants.Operators.MinusAssign:
                            case  Constants.Operators.MultiplyAssign:
                            case  Constants.Operators.DivideAssign:
                                return new AssignNode(left, op, ParseExprWithCheck(false));
                            
                            case Constants.Operators.OpenParenthesis:
                                //procedure call
                                var paramList = ParseParamList();
                                return new ProcedureCallNode(left, paramList);
                        }
                        break;
                }
                
                _lexer.Retract();
                throw Illegal(nextToken);
        }

        throw Illegal(t);
    }

    private ForNode ParseForStatement() {
        Require(Constants.Words.For);

        var initialVariableToken = _lexer.GetNextToken();
        IdentifierNode initialVariable;
        // initial
        if (initialVariableToken is IdentifierToken idToken) {
            initialVariable = new IdentifierNode(idToken); 
        }
        else {
            throw Illegal(initialVariableToken);
        }
        
        // assign
        var assignOperatorToken = _lexer.GetNextToken();

        AssignNode assignOperator;
        if (assignOperatorToken is OperatorToken op && op.Value == Constants.Operators.Assign) {
            assignOperator = new AssignNode(initialVariable, op, ParseExprWithCheck(false));
        }
        else {
            throw Illegal(assignOperatorToken); 
        }

        // direction
        var directionToken = _lexer.GetNextToken();
        ForNode.DirectionType direction;
        
        if (Check(directionToken, Constants.Words.To)) {
            direction = ForNode.DirectionType.To;
        } 
        else if (Check(directionToken, Constants.Words.Downto)) {
            direction = ForNode.DirectionType.Downto;
        }
        else {
            throw Illegal(directionToken);
        }
        
        
        // final value
        var finalValue = ParseExprWithCheck(false);
        
        Require(Constants.Words.Do);

        _cyclesCounter += 1;
        var statement = ParseStatementWithCheck();
        _cyclesCounter -= 1;
        return new ForNode(assignOperator, direction, finalValue, statement); 
    }

    private WhileNode ParseWhileStatement() {
        Require(Constants.Words.While);
        var condition = ParseExprWithCheck(false);
        Require(Constants.Words.Do);
        _cyclesCounter += 1;
        var st = ParseStatementWithCheck();
        _cyclesCounter -= 1;
        
        return new WhileNode(condition, st);
    }

    private IfNode ParseIfStatement() {
        Require(Constants.Words.If);
        var condition = ParseExprWithCheck(false);
        Require(Constants.Words.Then);
        var trueStatement = ParseStatementWithCheck();
                        
        if (Check(_lexer.GetNextToken(), Constants.Words.Else)) {
            var falseStatement = ParseStatementWithCheck();
            return new IfNode(condition, trueStatement, falseStatement);
        }
        _lexer.Retract();
        return new IfNode(condition, trueStatement);
    }
    
    private enum ParseParamListStates {Start, AfterFirst}
    // parse (expr {, expr})
    // starts after (->[..]
    private List<ExprNode> ParseParamList() {
        var parameters = new List<ExprNode>();

        var state = ParseParamListStates.Start;

        while (true) {
            var t = _lexer.GetNextToken();
            
            switch (state) {
                case ParseParamListStates.Start:
                    if (Check(t, Constants.Operators.CloseParenthesis))
                        return parameters;
                    _lexer.Retract();
                    
                    state = ParseParamListStates.AfterFirst;
                    parameters.Add(ParseExprWithCheck(false));
                    break;
                
                case ParseParamListStates.AfterFirst:
                    if (Check(t, Constants.Operators.CloseParenthesis))
                        return parameters;
                    _lexer.Retract();
                    
                    Require(Constants.Separators.Comma);
                    parameters.Add(ParseExprWithCheck(false));
                    break;
            }
            
        }
    }
    
    private enum ParseVarRefStates {Start, AfterDot, AfterBracket, AfterParenthesis}
    // id {.id | [expression] | (arg,...)}, 
    private ExprNode ParseVariableReference() {
        // ASK: really?
        ExprNode varRef;
        
        //get first mandatory identifier
        Token t;
        switch (t = _lexer.GetNextToken()) {
            case IdentifierToken identifier:
                varRef = new IdentifierNode(identifier);
                break;
            default:
                _lexer.Retract();
                throw Illegal(t);
        }

        var state = ParseVarRefStates.Start;

        //parse variable part
        while (true) {
            t = _lexer.GetNextToken();
            
            switch (state) {
                
                case ParseVarRefStates.Start:

                    if (Check(t, Constants.Operators.Dot))
                        state = ParseVarRefStates.AfterDot;
                    else if (Check(t, Constants.Operators.OpenBracket))
                        state = ParseVarRefStates.AfterBracket;
                    else if (Check(t, Constants.Operators.OpenParenthesis))
                        state = ParseVarRefStates.AfterParenthesis;
                    else if (t is OperatorToken op && op.Value == Constants.Operators.Caret) {
                        varRef = new UnaryOperationNode(op, varRef);
                    }
                    else {
                        _lexer.Retract();
                        return varRef;
                    } 
                    break;
                
                case ParseVarRefStates.AfterDot:

                    switch (t) {
                        case IdentifierToken identifierToken:
                            state = ParseVarRefStates.Start;
                            varRef = new AccessNode(varRef, identifierToken);
                            break;
                        default:
                            throw Illegal(t);
                    }
                    break;
                    
                case ParseVarRefStates.AfterBracket:
                    _lexer.Retract();
                    var expr = ParseExprWithCheck(false);
                    varRef = new IndexNode(varRef, expr);
                    
                    // [index1, index2][index3] is also allowed
                    while (Check(_lexer.GetNextToken(), Constants.Separators.Comma)) {
                        varRef = new IndexNode(varRef, ParseExprWithCheck(false));
                    }
                    
                    _lexer.Retract();
                    Require(Constants.Operators.CloseBracket);
                    state = ParseVarRefStates.Start;
                    break;
                
                case ParseVarRefStates.AfterParenthesis:
                    _lexer.Retract();
                    var paramList = ParseParamList();
                    varRef = new FunctionCallNode(varRef, paramList);
                    state = ParseVarRefStates.Start;
                    break;
            }
        }
    }

    private ExprNode ParseExpr(bool isConst) {
        var expr = ParseSimpleExpr(isConst);
        
        while (true) {
            var op = _lexer.GetNextToken();

            if (!Check<ConditionalOperatorsTokenGroup>(op)) {
                _lexer.Retract();
                break;
            }
            expr = new BinaryExprNode(expr, ParseSimpleExpr(isConst), op);
        }

        return expr;
    }
    
    private ExprNode ParseExprWithCheck(bool isConst) {
        var exp = ParseExpr(isConst);
        if (_checkSemantics)
            exp.Accept(_semanticsVisitor);
        return exp;
    }

    private ExprNode ParseSimpleExpr(bool isConst, int priority = 0) {
        if (priority >= TokenPriorities.Length) {
            var factor = ParseFactor(isConst);
            
            // check for dereferencing (^)
            if (isConst)
                return factor;
            
            var next = _lexer.GetNextToken();
            if (next is OperatorToken op && op.Value == Constants.Operators.Caret)
                return new UnaryOperationNode(op, factor);
                
            _lexer.Retract();

            return factor;
        }
        
        var node = ParseSimpleExpr(isConst, priority + 1);

        while (true) {
            var op = _lexer.GetNextToken();
            if (!TokenPriorities[priority].Contains(op)) {
                _lexer.Retract();
                break;
            }
            
            var right = ParseSimpleExpr(isConst, priority + 1);
            node = new BinaryExprNode(node, right, op);
        }

        return node;
    }

    // (expr), float, Integer, Identifier
    private ExprNode ParseFactor(bool isConst) {
        var t = _lexer.GetNextToken();
        
        switch (t) {
            case OperatorToken operatorToken:
                switch (operatorToken.Value) {
                    case Constants.Operators.OpenParenthesis:
                        var exp = ParseExprWithCheck(isConst);
                        Require(Constants.Operators.CloseParenthesis);
                        return exp;
                    //unary plus,minus, not
                    case Constants.Operators.Plus:
                    case Constants.Operators.Minus:
                        return new UnaryOperationNode(operatorToken, ParseFactor(isConst));
                    
                    case Constants.Operators.AtSign:
                        if (isConst) {
                            _lexer.Retract();
                            throw Illegal(t);
                        }
                        return new UnaryOperationNode(operatorToken, ParseVariableReference());
                }
                break;
            
            case FloatToken floatToken:
                return new FloatNode(floatToken);
            case IntegerToken integerToken:
                return new IntegerNode(integerToken);
            case StringToken stringToken:
                if (stringToken.StringValue.Length == 1)
                    return new CharNode(stringToken);
                else 
                    return new StringNode(stringToken); 
            
            case ReservedToken reservedToken:
                switch (reservedToken.Value) {
                    case Constants.Words.Not:
                        return new UnaryOperationNode(reservedToken, ParseExprWithCheck(isConst));
                }
                break;
            
            case IdentifierToken identityToken:
                // identifier, access or index or typecast
                _lexer.Retract();
                if (isConst)
                    throw Illegal(t);
                
                return ParseVariableReference();
           default:
                throw Illegal(t);
        }
        
        _lexer.Retract();
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

    private void Require(Constants.Operators op)  {
        var t = _lexer.GetNextToken();

        if (!Check(t, op)) {
            _lexer.Retract();
            throw new IllegalExprException(t.Lexeme, t.Line, t.Column, op.ToString());            
        }
        
    }

    private void Require(Constants.Words word) {
        var t = _lexer.GetNextToken();
        
        if (!Check(t, word)) {
            _lexer.Retract();
            throw new IllegalExprException(t.Lexeme, t.Line, t.Column, word.ToString());
        }
    }

    private void Require(Constants.Separators sep) {
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

    private void Optional(Constants.Separators sep) {
        var t = _lexer.GetNextToken();
        if (Check(t, sep))
            return;
        _lexer.Retract();
    }
    
    private ParserException Illegal(Token token) {
        return new IllegalExprException(token.Lexeme, token.Line, token.Column);
    }

    private bool Check(Token t, Constants.Operators op) {
        return t is OperatorToken _op && _op.Value == op;
        
    }

    private bool Check(Token t, Constants.Words word) {
        return t is ReservedToken _w && _w.Value == word;
    }
    
    private bool Check(Token t, Constants.Separators sep) {
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

public class ConditionalOperatorsTokenGroup : TokenGroup {
    public override bool Contains(Token token) {
        switch (token) {
            case OperatorToken op:
                switch (op.Value) {
                    case Constants.Operators.Less:
                    case Constants.Operators.LessOrEqual:
                    case Constants.Operators.More:
                    case Constants.Operators.MoreOreEqual:
                    case Constants.Operators.Equal:
                    case Constants.Operators.NotEqual:
                        return true;
                }
                break;
        }

        return false;
    }
}

public class TermTokenGroup : TokenGroup {
    public override bool Contains(Token token) {
        switch (token) {
            case OperatorToken opToken:
                switch (opToken.Value) {
                    case Constants.Operators.Plus:
                    case Constants.Operators.Minus:
                        return true;
                }
                break;
            
            case ReservedToken reservedToken:
                switch (reservedToken.Value) {
                    case Constants.Words.Or:
                    case Constants.Words.Xor:
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
                    case Constants.Operators.Multiply:
                    case Constants.Operators.Divide:
                        return true;
                }
                break;
            
            case ReservedToken reservedToken:
                switch (reservedToken.Value) {
                    case Constants.Words.Div:
                    case Constants.Words.Mod:
                    case Constants.Words.And:
                    case Constants.Words.Shl:
                    case Constants.Words.Shr:
                        return true;
                }
                break;
        }

        return false;
    }
}
}
