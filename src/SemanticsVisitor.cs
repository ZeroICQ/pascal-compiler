using System;
using System.Data;
using System.Diagnostics;
using System.Reflection;

namespace Compiler {

// sets Type values, makes implicit typecasts, 
public class SemanticsVisitor : IAstVisitor<bool> {
    private readonly SymStack _stack;
    private readonly TypeChecker _typeChecker;

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

    public bool Visit(FloatNode node) {
        if (node.Type != null) return true;
        
        node.Type = _stack.SymFloat;
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

        //todo: rmk when add pointers
        if (!(node.Name is IdentifierNode funcIdentifier)) {
            node.Name.Accept(this);
            var t = ExprNode.GetClosestToken(node);
            throw new FunctionExpectedException(node.Name.Type, t.Lexeme, t.Line, t.Column);
        }

        var funcSym = _stack.FindFunction(funcIdentifier.Token.Value);
        
        if (funcSym == null)
            throw new IdentifierNotDefinedException(funcIdentifier.Token.Lexeme, funcIdentifier.Token.Line, funcIdentifier.Token.Column);
        
        foreach (var arg in node.Args) {
            arg.Accept(this);
        }
        
        var symbol = _typeChecker.RequireFunction(funcIdentifier.Token, funcSym, node.Args);
        node.Type = symbol.ReturnType;
        node.Symbol = symbol;
        //todo: function cal return lvalues
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
        if (!(node.Operand.Type is SymArray arrayType))
            throw new ArrayExpectedException(node.Operand.Type, token.Lexeme, token.Line, token.Column);

        node.Type = arrayType.Type;
        node.IsLvalue = true;
        
        _typeChecker.RequireCast(_stack.SymInt, ref node.IndexExpr);
        
        //try to compute index value
        var indexEvalVisitor = new EvalConstExprVisitor(ExprNode.GetClosestToken(node.IndexExpr), _stack);
        try {
            var indexConst = node.IndexExpr.Accept(indexEvalVisitor) as SymIntConst;
            // requireCast above guarantees that it is not null
            node.IndexExpr = new IntegerNode(new IntegerToken(indexConst.Value, token.Line, token.Column));

            if (!(arrayType.MinIndex.Value <= indexConst.Value && indexConst.Value <= arrayType.MaxIndex.Value))
                throw new RangeCheckErrorException(indexConst.Value, arrayType.MinIndex.Value, 
                    arrayType.MaxIndex.Value, token.Line, token.Column);

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
            if (!(node.Expr.Type is SymScalar) || node.Expr.Type is SymFloat)
                throw BuildException<OperatorNotOverloaded>(node.Expr.Type, node.Operation);
        }
        node.Type = node.Expr.Type;
        return true;
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