using System;
using System.Reflection;

namespace Compiler {

// sets Type values, makes implicit typecasts, 
public class SemanticsVisitor : IAstVisitor<bool> {
    private readonly SymStack _stack;
    private readonly TypeChecker _typeChecker;
    
    //not null if inside function
    public bool IsInsideFunction { get; set; } = false;
    //null - procedure
    public SymType FunctionReturnType { get; set; } = null; 

    public SemanticsVisitor(SymStack stack, TypeChecker typeChecker) {
        _stack = stack;
        _typeChecker = typeChecker;
    }
    
    public bool Visit(BlockNode node) {
        foreach (var statement in node.Statements) {
            statement.Accept(this);
        }

        return true;
    }

    public bool Visit(BinaryExprNode node) {
        if (node.Type != null)
            return true;
        
        node.Left.Accept(this);
        node.Right.Accept(this);
         
        _typeChecker.RequireBinaryAny(ref node.Left, ref node.Right, node.Operation);

        switch (node.Operation) {
            case OperatorToken op: 
                switch (op.Value) {
                    case Constants.Operators.Less:
                    case Constants.Operators.LessOrEqual:
                    case Constants.Operators.More:
                    case Constants.Operators.MoreOreEqual:
                    case Constants.Operators.Equal:
                    case Constants.Operators.NotEqual:
                        node.Type = _stack.SymBool;
                        return true;
//                    case Constants.Operators.Divide:
                }
                break;
        }
        // left and right nodes should be same type by this point
        node.Type = node.Left.Type;
        return true;
    }

    public bool Visit(IntegerNode node) {
        if (node.Type != null) return true;
        
        node.Type = _stack.SymInt;
        return true;
    }

    public bool Visit(DoubleNode node) {
        if (node.Type != null) return true;
        
        node.Type = _stack.SymDouble;
        return true;
    }

    public bool Visit(IdentifierNode node) {
        if (node.Type != null) return true;
        
        var sym = _stack.FindVarOrConst(node.Token.Value);
        if (sym == null)
            throw BuildException<IdentifierNotDefinedException>(node.Token);
        
        node.Symbol = sym;
        node.Type = sym.Type;
        node.IsLvalue = !sym.IsConst;
        
        return true;
    }

    public bool Visit(FunctionCallNode node) {
        if (node.Type != null) return true;

        node.Name.Accept(this);
        //todo: rmk when add pointers
        
        if (!(node.Name.Type is SymFunc funcIdentifier)) {
            var t = ExprNode.GetClosestToken(node);
            throw new FunctionExpectedException(node.Name.Type, t.Lexeme, t.Line, t.Column);
        }
        

        foreach (var arg in node.Args) {
            arg.Accept(this);
        }
        
        //the very special crutches 
        if (node.Name.Type is ExitSymFunc exit) {
            if (FunctionReturnType == null) {
                if (node.Args.Count != 0) {
                    var t = ExprNode.GetClosestToken(node.Name);
                    throw new WrongArgumentsNumberException(0, node.Args.Count, t.Lexeme, t.Line, t.Column);
                }
            }
            else {
                if (node.Args.Count != 1) {
                    var t = ExprNode.GetClosestToken(node.Name);
                    throw new WrongArgumentsNumberException(0, node.Args.Count, t.Lexeme, t.Line, t.Column);
                }

                var tmp = node.Args[0];
                _typeChecker.RequireCast(FunctionReturnType, ref tmp);
                node.Args[0] = tmp;
            }

            node.Symbol = _stack.ExitFunc;
            return true;
        }

        if (node.Name.Type is HighSymFunc high) {
            node.Symbol = _stack.HighFunc; 
            node.Type = _stack.HighFunc.ReturnType; 
            var t = ExprNode.GetClosestToken(node.Name);
            if (node.Args.Count != 1) {
                throw new WrongArgumentsNumberException(1, node.Args.Count, t.Lexeme, t.Line, t.Column);
            }

            if (!(node.Args[0].Type is SymArray || node.Args[0].Type is OpenArray)) {
                throw new IllegalExprException(t.Lexeme, t.Line, t.Column);
            }

            return true;
        }

        if (node.Name.Type is LowSymFunc low) {
            node.Symbol = _stack.LowFunc; 
            node.Type = _stack.LowFunc.ReturnType;
            
            var t = ExprNode.GetClosestToken(node.Name);
            if (node.Args.Count != 1) {
                throw new WrongArgumentsNumberException(1, node.Args.Count, t.Lexeme, t.Line, t.Column);
            }

            if (!(node.Args[0].Type is SymArray || node.Args[0].Type is OpenArray)) {
                throw new IllegalExprException(t.Lexeme, t.Line, t.Column);
            }
            
            //inline functions add here

            return true;
        }
        
        var symbol = _typeChecker.RequireFunction(ExprNode.GetClosestToken(node.Name), funcIdentifier, node.Args);
        node.Type = symbol.ReturnType;
        node.Symbol = symbol;
        return true;
    }

    public bool Visit(ProcedureCallNode node) {
        node.Function.Accept(this);
        return true;
    }

    public bool Visit(CastNode node) {
        if (node.Type != null) return true;
        node.Expr.Accept(this);
        
        if (!_typeChecker.CanCast(node.CastTo, node.Expr)) {
            var t = ExprNode.GetClosestToken(node.Expr);
            throw new IncompatibleTypesException(node.CastTo, node.Expr.Type, t.Lexeme, t.Line, t.Column);
        }

        node.Type = node.CastTo;
        return true;
    }

    public bool Visit(AccessNode node) {
        if (node.Type != null) return true;
        node.Name.Accept(this);
        node.Type  = _typeChecker.RequireAccess(node.Name, node.Field);
        node.IsLvalue = true;
        
        return true;
    }

    public bool Visit(IndexNode node) {
        if (node.Type != null) return true;
        
        node.Operand.Accept(this);
        node.IndexExpr.Accept(this);

        var token = ExprNode.GetClosestToken(node.Operand);
        
        if (node.Operand.Type is SymArray arrayType) {
            node.Type = arrayType.Type;
        }
        else if (node.Operand.Type is OpenArray openArrayType){
            node.Type = openArrayType.InnerType;
        }
        else {    
            throw new ArrayExpectedException(node.Operand.Type, token.Lexeme, token.Line, token.Column);
        }
        
        node.IsLvalue = true;
        
        _typeChecker.RequireCast(_stack.SymInt, ref node.IndexExpr);
        
        //try to compute index value
        var indexEvalVisitor = new EvalConstExprVisitor(ExprNode.GetClosestToken(node.IndexExpr), _stack);
        try {
            var indexConst = node.IndexExpr.Accept(indexEvalVisitor) as SymIntConst;
            // requireCast above guarantees that it is not null
            node.IndexExpr = new IntegerNode(new IntegerToken(indexConst.Value, token.Line, token.Column));

            if (!(node.Operand.Type is SymArray arrayT)) {
                return true;
            }

            if (!(arrayT.MinIndex.Value <= indexConst.Value && indexConst.Value <= arrayT.MaxIndex.Value))
                throw new RangeCheckErrorException(indexConst.Value, arrayT.MinIndex.Value, 
                    arrayT.MaxIndex.Value, token.Line, token.Column);

        }
        catch (ConstExprEvalException) {
            return true;
        }

        return true;
    }

    public bool Visit(StringNode node) {
        if (node.Type != null) return true;
        
        node.Type = _stack.SymString;
        return true;
    }

    public bool Visit(UnaryOperationNode node) {
        //todo: special treat with pointers ^, @
        if (node.Type != null) return true;
        
        node.Expr.Accept(this);

        // constraints to not
        if (node.Operation is ReservedToken reservedToken && reservedToken.Value == Constants.Words.Not) {
            if (!(node.Expr.Type is SymInt || node.Expr.Type is SymBool || node.Expr.Type is SymChar))
                throw BuildException<OperatorNotOverloaded>(node.Expr.Type, node.Operation);
            
            node.Type = node.Expr.Type;
            return true;
        }

        node.Type = node.Expr.Type;
        
        switch (node.Operation) {
            case OperatorToken op:
                switch (node.Expr.Type) {
                    case SymInt _:
                    case SymDouble _:
                        return true;
                }
                break;
            
            case ReservedToken word:
                switch (word.Value) {
                    case Constants.Words.Not:
                        
                            switch (node.Type) {
                                case SymInt _:
                                    return true;
                            }
                            
                        break;
                }
                break;
        }
        
        throw BuildException<OperatorNotOverloaded>(node.Expr.Type, node.Operation);
    }

    public bool Visit(AssignNode node) {
        node.Left.Accept(this);
        node.Right.Accept(this);
        
        if (!node.Left.IsLvalue)
            throw BuildException<NotLvalueException>(ExprNode.GetClosestToken(node.Left));
        
        _typeChecker.RequireAssignment(ref node.Left, ref node.Right, node.Operation);
        return true;
    }

    public bool Visit(IfNode node) {
        node.Condition.Accept(this);
        _typeChecker.RequireCast(_stack.SymBool, ref node.Condition);
        return true;
    }

    public bool Visit(WhileNode node) {
        node.Condition.Accept(this);
        _typeChecker.RequireCast(_stack.SymBool, ref node.Condition);        
        return true;
    }

    public bool Visit(ForNode node) {
        node.Initial.Accept(this);
        node.Final.Accept(this);
        
        _typeChecker.RequireCast(_stack.SymInt, ref node.Initial.Left);
        _typeChecker.RequireCast(_stack.SymInt, ref node.Final);
        
        return true;
    }

    // continue, break, return
    public bool Visit(ControlSequence node) {
        return true;
    }

    public bool Visit(EmptyStatementNode node) {
        return true;
    }

    public bool Visit(CharNode node) {
        if (node.Type != null) return true;

        node.Type = _stack.SymChar;
        return true;
    }

    public bool Visit(WriteStatementNode node) {
        //todo: rmk when add pointers
        
        foreach (var arg in node.Args) {
            arg.Accept(this);
            
            var realType = arg.Type;
                        
            if (realType is SymTypeAlias symTypeAlias)
                realType = symTypeAlias.Type;
            
            switch (realType) {
                case SymInt _:
                case SymChar _:
                case SymString _:
                case SymDouble _:
                case SymBool _:
                    break;
                default:
                    var t = ExprNode.GetClosestToken(arg);
                    throw new WritelnUnsupportedType(realType, t.Lexeme, t.Line, t.Column);
            }
        }
        //todo: function cal return lvalues
        return true;
    }

    private static T BuildException<T>(Token token) where T : ParserException {
        try {
            return (T)Activator.CreateInstance(typeof(T), token.Lexeme, token.Line, token.Column);
        }
        catch (TargetInvocationException e) {
            throw e.InnerException;
        }
    }
    
    private static T BuildException<T>(SymType type, Token token) where T : ParserException {
        try {
            return (T)Activator.CreateInstance(typeof(T), type, token.Lexeme, token.Line, token.Column);
        }
        catch (TargetInvocationException e) {
            throw e.InnerException;
        }
    }
    
}
}