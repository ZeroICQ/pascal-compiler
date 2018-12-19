using System;
using System.Reflection;

namespace Compiler {

// sets Type values, makes implicit typecasts, 
public class SemanticsVisitor : IAstVisitor<bool> {
    private readonly SymStack _stack;
    private readonly TypeChecker _typeChecker;

    public SemanticsVisitor(SymStack stack) {
        _stack = stack;
        _typeChecker = new TypeChecker(stack);
    }
    
    public bool Visit(BlockNode node) {
        foreach (var statement in node.Statements) {
            statement.Accept(this);
        }

        return true;
    }

    public bool Visit(BinaryExprNode node) {
        if (node.Left.Type == null)
            node.Left.Accept(this);

        if (node.Right.Type == null)
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
        node.Type = _stack.SymInt;
        return true;
    }

    public bool Visit(FloatNode node) {
        node.Type = _stack.SymFloat;
        return true;
    }

    public bool Visit(IdentifierNode node) {
        var sym = _stack.FindVarOrConst(node.Token.Value);
        if (sym == null)
            throw BuildException<IdentifierNotDefinedException>(node.Token);
        
        node.Symbol = sym;
        node.Type = sym.Type;
        node.IsLvalue = !sym.IsConst;
        
        return true;
    }

    public bool Visit(FunctionCallNode node) {
        foreach (var arg in node.Args) {
            arg.Accept(this);
        }
        
        switch (node.Name) {
            
            case IdentifierNode idNode:

                if (node.Args.Count == 1 &&_stack.FindType(idNode.Token.Value) is SymType type) {
                    var castingExpr = node.Args[0];
                    
                    if (!_typeChecker.CanCast(type, ref castingExpr)) {
                        throw new IncompatibleTypesException(type, castingExpr.Type, idNode.Token.Lexeme,
                            idNode.Token.Line, idNode.Token.Column);
                    }
                }
                
                break;
        }

        return true;
    }

    public bool Visit(CastNode node) {
        // is never created in parser because is is impossible to distinguish from func/proc 
        throw new System.NotImplementedException();
    }

    public bool Visit(AccessNode node) {
        throw new System.NotImplementedException();
    }

    public bool Visit(IndexNode node) {
        throw new System.NotImplementedException();
    }

    public bool Visit(StringNode node) {
        node.Type = _stack.SymString;
        return true;
    }

    public bool Visit(UnaryOperationNode node) {
        //todo: special treat with pointers ^, @
        
        if (node.Expr.Type == null)
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
        if (node.Left.Type == null)
            node.Left.Accept(this);
        
        if (node.Right.Type == null)
            node.Right.Accept(this);
        
        if (!node.Left.IsLvalue)
            throw BuildException<NotLvalueException>(ExprNode.GetClosestToken(node.Left));
        
        _typeChecker.RequireAssignment(ref node.Left, ref node.Right, node.Operation);
        return true;
    }

    public bool Visit(IfNode node) {
        throw new System.NotImplementedException();
    }

    public bool Visit(WhileNode node) {
        throw new System.NotImplementedException();
    }

    public bool Visit(ProcedureCallNode node) {
        throw new System.NotImplementedException();
    }

    public bool Visit(ForNode node) {
        throw new System.NotImplementedException();
    }

    // continue, break, return
    public bool Visit(ControlSequence node) {
        return true;
    }

    public bool Visit(EmptyStatementNode node) {
        return true;
    }

    public bool Visit(CharNode node) {
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