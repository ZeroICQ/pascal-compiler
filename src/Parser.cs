using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Compiler {
public class Parser {
    private LexemesAutomata _lexer;
    private ulong _cyclesCounter = 0;
    private SymStack _symStack;
    private bool _checkSemantics;
    private TypeChecker _typeChecker;
    private SemanticsVisitor _semanticsVisitor;

    public Parser(LexemesAutomata lexer, bool checkSemantics) {
        _lexer = lexer;
        _symStack = new SymStack();
        _checkSemantics = checkSemantics;
        _typeChecker = new TypeChecker(_symStack);
        _semanticsVisitor = new SemanticsVisitor(_symStack, _typeChecker);
        
        // Global namespace
        _symStack.Push();
    }

    public (AstNode, SymStack) Parse() { 
        ParseDeclarations();
        
        var mainBlock = ParseCompoundStatement();
        Require(Constants.Operators.Dot);
        mainBlock.IsMain = true;
        return (mainBlock, _symStack);
    }


    private void ParseDeclarations() {
        while (true) {
            var t = _lexer.GetNextToken();
            
            if (Check(t, Constants.Words.Var)) {
                ParseVariableDeclarations();
                continue;
            }
            
            if (Check(t, Constants.Words.Type)) {
                ParseTypeDeclarations();
                continue;
            }

            if (Check(t, Constants.Words.Const)) {
               ParseConstDeclarations();
               continue;
            }

            if (Check(t, Constants.Words.Procedure)) {
                ParseFunctionDeclaration(true);
                continue;
            }
            
            if (Check(t, Constants.Words.Function)) {
                ParseFunctionDeclaration(false);
                continue;
            }
            
            break;
        }

        _lexer.Retract();
    }
    
    private void ParseFunctionDeclaration(bool isProcedure) {
        _symStack.Push();
        
        var t = _lexer.GetNextToken();
        if (!(t is IdentifierToken identifierToken)) {
            _lexer.Retract();
            throw Illegal(t);
        }

        var paramList = new List<SymVar>();
        
        t = _lexer.GetNextToken();
        
        if (!(t is SeparatorToken sep && sep.Value == Constants.Separators.Semicolon)) {
            _lexer.Retract();
            paramList = ParseParamList();
        }
        else {
            _lexer.Retract();
        }

        IdentifierToken returnTypeToken = null;
        
        //parse return type
        if (!isProcedure) {
            Require(Constants.Separators.Colon);
            t = _lexer.GetNextToken();
            if (t is IdentifierToken returnIdentifier) {
                returnTypeToken = returnIdentifier;
            }
            else {
                throw Illegal(t);
            }
        }
        
        Require(Constants.Separators.Semicolon);
        t = _lexer.GetNextToken();
        //local variables
        if (t is ReservedToken reservedToken && reservedToken.Value == Constants.Words.Var) {
            //todo: make parameters not globals
            ParseVariableDeclarations(SymVar.VarTypeEnum.Local);
        }
        else {
            _lexer.Retract();
        }

        var body = ParseStatementWithCheck();
        Require(Constants.Separators.Semicolon);
        
        var localVars = _symStack.Pop();
        _symStack.AddFunction(identifierToken, paramList, localVars, body, returnTypeToken);
    }

    private List<SymVar> ParseParamList() {
        Require(Constants.Operators.OpenParenthesis);
        var paramList = new List<SymVar>();
        
        while (true) {
            var t = _lexer.GetNextToken();
            
            //determine type var/const/out/<no modificator>
            var paramModifier = SymVar.VarTypeEnum.Parameter;
            switch (t) {
                case ReservedToken reservedToken:
                    switch (reservedToken.Value) {
                        case Constants.Words.Var:
                            paramModifier = SymVar.VarTypeEnum.VarParameter;
                            break;
                        
                        case Constants.Words.Const:
                            paramModifier = SymVar.VarTypeEnum.ConstParameter;
                            break;
                        case Constants.Words.Out:
                            paramModifier = SymVar.VarTypeEnum.OutParameter;
                            break;                    
                    default:
                        _lexer.Retract();
                        break;
                    }                   
                    break;
                
                case OperatorToken opToken:
                    switch (opToken.Value) {
                        case Constants.Operators.CloseParenthesis:
                            return paramList;
                        default:
                            _lexer.Retract();
                            break;
                    }
                    break;

                default:
                    _lexer.Retract();
                    break;
            }

            //parse parameter names. single or separated with comma
            var identifiersTokens = new List<IdentifierToken>();
            
            while (true) {
                t = _lexer.GetNextToken();
                
                if (!(t is IdentifierToken paramIdentifierToken))
                    throw Illegal(t);

                identifiersTokens.Add(paramIdentifierToken);
                
                t = _lexer.GetNextToken();
                if (t is SeparatorToken sep && sep.Value == Constants.Separators.Comma) 
                    continue;
                
                _lexer.Retract();
                break;
            }
            Require(Constants.Separators.Colon);            
            //parse parameter(s) type
            t = _lexer.GetNextToken();
            SymType paramType = null;
            
            switch (t) {
                case ReservedToken reservedToken:
                    switch (reservedToken.Value) {
                        case Constants.Words.Array:
                            if (_lexer.GetNextToken() is ReservedToken res && res.Value == Constants.Words.Of) {
                                Require(Constants.Words.Const);
                                paramType = ArrayOfConst.Instance;
                            }
                            else {
                                //some other array
                                _lexer.Retract();
                                paramType = ParseArrayTypeDeclaration(true);
                            }
                            break;
                    }
                    break;
                
                case IdentifierToken identifierToken:
                    paramType = _symStack.FindType(identifierToken.Value);
                    if (paramType == null)
                        throw new TypeNotFoundException(identifierToken.Lexeme, identifierToken.Line, identifierToken.Column);
                    break;
            }
            
            //todo: default values
            foreach (var idToken in identifiersTokens) {
                paramList.Add(_symStack.AddVariable(idToken, paramType, paramModifier));
            }

            t = _lexer.GetNextToken();
            if (t is SeparatorToken sp && sp.Value == Constants.Separators.Semicolon)
                    continue;

            if (t is OperatorToken op && op.Value == Constants.Operators.CloseParenthesis)
                return paramList;
            
            _lexer.Retract();
            throw Illegal(t);
        }
    }

    // starts after "type"
    private void ParseTypeDeclarations() {
        var isFirst = true;

        while (true) {
            var next = _lexer.GetNextToken();

            if (next is IdentifierToken nameToken) {
                Require(Constants.Operators.Equal);

                next = _lexer.GetNextToken();

                if (next is IdentifierToken typeToken) {
                    Require(Constants.Separators.Semicolon);
                    _symStack.AddAlias(nameToken, typeToken);
                }
                else if (Check(next, Constants.Words.Type)) {
                    next = _lexer.GetNextToken();

                    if (next is IdentifierToken aliasTypeToken) {
                        Require(Constants.Separators.Semicolon);
                        _symStack.AddAliasType(nameToken, aliasTypeToken);
                    }
                    else {
                        _lexer.Retract();
                        _symStack.AddAliasType(nameToken, ParseArrayTypeDeclaration());
                        Require(Constants.Separators.Semicolon);
                    }
                }
                else if (Check(next, Constants.Words.Record)) {
                    _symStack.AddType(ParseRecord(nameToken.Value), nameToken);
                }
                else {
                    _lexer.Retract();
                    _symStack.AddAlias(nameToken, ParseArrayTypeDeclaration());
                    Require(Constants.Separators.Semicolon);
                }

            } 
            else if (isFirst) {
                _lexer.Retract();
                throw Illegal(next);
            }
            else {
                _lexer.Retract();
                return;
            }

            if (isFirst)
                isFirst = false;
        }
    }

    // starts after type <recrdr name> = record ->[...]
    private SymRecord ParseRecord(string name) {
        _symStack.Push();
        
        //empty
        if (Check(_lexer.GetNextToken(), Constants.Words.End)) {
            Require(Constants.Separators.Semicolon);
            return new SymRecord(name, _symStack.Pop());
        }
        _lexer.Retract();
            
        ParseVariableDeclarations();
        var fields = _symStack.Pop();
        Require(Constants.Words.End);
        Require(Constants.Separators.Semicolon);
        
        return new SymRecord(name, fields);
    }
    
    // start after "const"
    private void ParseConstDeclarations() {
        var isFirst = true;

        while (true) {
            var tmpToken = _lexer.GetNextToken();
            IdentifierToken identifier;
            
            if (tmpToken is IdentifierToken id) {
                identifier = id;
            }
            else if (isFirst){
                _lexer.Retract();
                throw Illegal(tmpToken);
            }
            else {
                _lexer.Retract();
                return;
            }

            if (isFirst)
                isFirst = false;

            Require(Constants.Operators.Equal);
            var initialExpr = ParseExprWithCheck(true);
            Require(Constants.Separators.Semicolon);
            
            var evalVisitor = new EvalConstExprVisitor(identifier, _symStack);
            var symConst = initialExpr.Accept(evalVisitor);
            
            _symStack.AddConst(identifier, symConst);
        }
    }

    // start after "var"
    private enum ParseVariableDeclarationsStates {Start, SingleVariable, MultipleVariables} 
    private void ParseVariableDeclarations(SymVar.VarTypeEnum varType = SymVar.VarTypeEnum.Global) {
        var state = ParseVariableDeclarationsStates.Start;
        
        var identifiers = new List<IdentifierToken>();

        var isFirst = true;
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
                //after "var <identifier> :"->[...]
                case ParseVariableDeclarationsStates.SingleVariable:
                    if (t is IdentifierToken typeToken) {
                        SymConst initialValue = null;

                        if (Check(_lexer.GetNextToken(), Constants.Operators.Equal)) {
                            var initialExpr = ParseExprWithCheck(true);

                            var type = _symStack.FindType(typeToken.Value);

                            if (type == null) {
                                throw new IdentifierNotDefinedException(typeToken.Lexeme, typeToken.Line,
                                    typeToken.Column);
                            }

                            var initialValueToken = ExprNode.GetClosestToken(initialExpr);
                            var initValEvalVisitor = new EvalConstExprVisitor(initialValueToken, _symStack);
                            initialValue = initialExpr.Accept(initValEvalVisitor);
                            _typeChecker.RequireCast(type, ref initialExpr);
                        }
                        else
                            _lexer.Retract();    
                        
                        _symStack.AddVariable(identifiers[0], typeToken, varType, initialValue);
                        Require(Constants.Separators.Semicolon);
                        isFirst = false;
                        state = ParseVariableDeclarationsStates.Start;
                        break;
                    }
                    else if (t is ReservedToken) {
                        _lexer.Retract();
                        var arrayType = ParseArrayTypeDeclaration();
                        Require(Constants.Separators.Semicolon);
                        _symStack.AddArray(identifiers[0], arrayType, varType);
                        
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
                                    _symStack.AddVariable(id, tpToken, varType);
                                }
                                Require(Constants.Separators.Semicolon);
                                isFirst = false;
                                state = ParseVariableDeclarationsStates.Start;
                                break;
                            }
                            else if (typeT is ReservedToken) {
                                _lexer.Retract();
                                var arrayType = ParseArrayTypeDeclaration();
                                Require(Constants.Separators.Semicolon);
                                
                                foreach (var id in identifiers) {
                                    _symStack.AddArray(id, arrayType, varType);
                                }
                                
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

    private SymArray ParseArrayTypeDeclaration(bool skipArrayWord = false) {
        if (!skipArrayWord) {
            Require(Constants.Words.Array);
        }
        Require(Constants.Operators.OpenBracket);

        var startIndexExpr = ParseExprWithCheck(true);
        var startIndexEvalVisitor = new EvalConstExprVisitor(ExprNode.GetClosestToken(startIndexExpr), _symStack);
        var startIndexConst = startIndexExpr.Accept(startIndexEvalVisitor);
        var startIndexIntConst = startIndexConst as SymIntConst;

        if (startIndexIntConst == null) {
            var t = ExprNode.GetClosestToken(startIndexExpr);
            throw new IncompatibleTypesException(_symStack.SymInt, startIndexConst.Type,
                startIndexConst.InitialStringValue, t.Line, t.Column);
        }

        Require(Constants.Operators.Dot);
        Require(Constants.Operators.Dot);

        var endIndexExpr = ParseExprWithCheck(true);
        var endIndexEvalVisitor = new EvalConstExprVisitor(ExprNode.GetClosestToken(endIndexExpr), _symStack);
        var endIndexConst = endIndexExpr.Accept(endIndexEvalVisitor);
        var endIndexIntConst = endIndexConst as SymIntConst;
        
        if (endIndexIntConst == null) {
            var t = ExprNode.GetClosestToken(endIndexExpr);
            throw new IncompatibleTypesException(_symStack.SymInt, endIndexExpr.Type,
                endIndexConst.InitialStringValue, t.Line, t.Column);
        }
                        
        Require(Constants.Operators.CloseBracket);
        Require(Constants.Words.Of);

        if (endIndexIntConst.Value < startIndexIntConst.Value) {
            var t = ExprNode.GetClosestToken(endIndexExpr);
            throw new UpperRangeBoundLessThanLowerException(t.Lexeme, t.Line, t.Column);
        }
        
        // array
        if (!(_lexer.GetNextToken() is IdentifierToken idToken)) {
            _lexer.Retract();
            return new SymArray(startIndexIntConst, endIndexIntConst, ParseArrayTypeDeclaration());
        }

        // else - type         
        var type = _symStack.FindType(idToken.Value);
        if (type == null)
            throw new IdentifierNotDefinedException(idToken.Lexeme, idToken.Line, idToken.Column);
        return new SymArray(startIndexIntConst, endIndexIntConst, type);
    }

    private StatementNode ParseStatementWithCheck() {
        var st = ParseStatement();
        if (_checkSemantics)
            st.Accept(_semanticsVisitor);
        return st;
    }
    
    // retracts
    private BlockNode ParseCompoundStatement(bool ignoreEmptyBlocks = true) {
        var compoundStatement = new BlockNode();
        
        Require(Constants.Words.Begin);
        var nextStmnt = ParseStatementWithCheck();
        
        if (!(ignoreEmptyBlocks && nextStmnt is EmptyStatementNode)) {
            compoundStatement.AddStatement(nextStmnt);
        }
        
//        compoundStatement.AddStatement(ParseStatementWithCheck());
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
            nextStmnt = ParseStatementWithCheck();
        
            if (!(ignoreEmptyBlocks && nextStmnt is EmptyStatementNode)) {
                compoundStatement.AddStatement(nextStmnt);
            }
        }
    }

    private StatementNode ParseStatement() {
        var t = _lexer.GetNextToken();
                
        switch (t) {
            // statements that starts with reserved words
            case SeparatorToken separatorToken:
                break;
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
                    case Constants.Words.Writeln:
                        _lexer.Retract();
                        return ParseWriteStatement(true);
                    case Constants.Words.Write:
                        _lexer.Retract();
                        return ParseWriteStatement(false);
                }
                break;
            
            // assignment
            default:
                _lexer.Retract();
                var left = ParseExprWithCheck(false);

                if (left is FunctionCallNode f) {
                    return new ProcedureCallNode(f);
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
                                var paramList = ParseArgumentList();
                                return new ProcedureCallNode(new FunctionCallNode(left, paramList));
                        }
                        break;
                }
//                break;
                //return new EmptyStatementNode();
                _lexer.Retract();
                throw Illegal(nextToken);
        }

        //todo: experiment
        _lexer.Retract();
        return new EmptyStatementNode();
//        throw Illegal(t);
    }

    //starts at "->write(ln)[...]"
    private WriteStatementNode ParseWriteStatement(bool isLn) {
        if (isLn)
            Require(Constants.Words.Writeln);
        else
            Require(Constants.Words.Write);
        var t = _lexer.GetNextToken();
        
        if (t is OperatorToken sep && sep.Value == Constants.Operators.OpenParenthesis) {
            return new WriteStatementNode(ParseArgumentList(), isLn);
        }
        
         _lexer.Retract();
        return new WriteStatementNode(new List<ExprNode>(), isLn);
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

        if (Check(_lexer.GetNextToken(), Constants.Separators.Semicolon)) {
            _lexer.Retract();
            return new IfNode(condition, trueStatement);
        }
        
        _lexer.Retract();

        if (Check(_lexer.GetNextToken(), Constants.Words.Else)) {
            var falseStatement = ParseStatementWithCheck();
            return new IfNode(condition, trueStatement, falseStatement);
        }
        _lexer.Retract();
        return new IfNode(condition, trueStatement);
    }
    
    private enum ParseArgListStates {Start, AfterFirst}
    // parse (expr {, expr})
    // starts after (->[..]
    private List<ExprNode> ParseArgumentList() {
        var args = new List<ExprNode>();

        var state = ParseArgListStates.Start;

        while (true) {
            var t = _lexer.GetNextToken();
            
            switch (state) {
                case ParseArgListStates.Start:
                    if (Check(t, Constants.Operators.CloseParenthesis))
                        return args;
                    _lexer.Retract();
                    
                    state = ParseArgListStates.AfterFirst;
                    args.Add(ParseExprWithCheck(false));
                    break;
                
                case ParseArgListStates.AfterFirst:
                    if (Check(t, Constants.Operators.CloseParenthesis))
                        return args;
                    _lexer.Retract();
                    
                    Require(Constants.Separators.Comma);
                    args.Add(ParseExprWithCheck(false));
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
                    var paramList = ParseArgumentList();
                    
                    //todo: need to distinguish function call and cast:
                    if (varRef is IdentifierNode idNode) {
                        var type = _symStack.FindType(idNode.Token.Value);
                        if (type != null && !(type is SymFunc) && paramList.Count == 1) {
                            varRef = new CastNode(type, paramList[0]);
                            
                            state = ParseVarRefStates.Start;
                            break;
                        }
                    }

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

    // (expr), Double, Integer, Identifier
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
            
            case DoubleToken doubleToken:
                return new DoubleNode(doubleToken);
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
                        return new UnaryOperationNode(reservedToken, ParseFactor(isConst));
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
