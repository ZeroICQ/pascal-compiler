using System;
using System.Collections;
using System.Reflection;

namespace Compiler {

// sets Type values, makes implicit typecasts, 
public class SemanticsVisitor : IAstVisitor<bool> {
    private readonly SymStack _stack;

    public SemanticsVisitor(SymStack stack) {
        _stack = stack;
    }
    
    public bool Visit(BlockNode node) {
        foreach (var statement in node.Statements) {
            statement.Accept(this);
        }

        return true;
    }

    public bool Visit(BinaryExprNode node) {
        throw new System.NotImplementedException();
    }

    public bool Visit(IntegerNode node) {
        throw new System.NotImplementedException();
    }

    public bool Visit(FloatNode node) {
        throw new System.NotImplementedException();
    }

    public bool Visit(IdentifierNode node) {
        var sym = _stack.FindVar(node.Token.Value);
        if (sym == null)
            throw BuildException<IdentifierNotDefinedException>(node.Token);
        node.Symbol = sym;
        node.Type = sym.Type;
        return true;
    }

    public bool Visit(FunctionCallNode node) {
        throw new System.NotImplementedException();
    }

    public bool Visit(CastNode node) {
        throw new System.NotImplementedException();
    }

    public bool Visit(AccessNode node) {
        throw new System.NotImplementedException();
    }

    public bool Visit(IndexNode node) {
        throw new System.NotImplementedException();
    }

    public bool Visit(StringNode node) {
        throw new System.NotImplementedException();
    }

    public bool Visit(UnaryOperationNode node) {
        throw new System.NotImplementedException();
    }

    public bool Visit(AssignNode node) {
        // todo replace with cheker + lval
        if (!ReferenceEquals(node.Left.Type, node.Right.Type))
            throw Incompatible(node.Left.Type, node.Right.Type, node.Right);
        
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

    public bool Visit(ControlSequence node) {
        throw new System.NotImplementedException();
    }

    public bool Visit(EmptyStatementNode node) {
        throw new System.NotImplementedException();
    }

    public bool Visit(CharNode node) {
        throw new System.NotImplementedException();
    }
    
    private static T BuildException<T>(Token token) where T : ParserException {
        try {
            return (T)Activator.CreateInstance(typeof(T), token.Lexeme, token.Line, token.Column);
        }
        catch (TargetInvocationException e) {
            throw e.InnerException;
        }
    }

    private static IncompatibleTypesException Incompatible(SymType left, SymType right, ExprNode node) {
        var token = GetLeftmostToken(node);
        return new IncompatibleTypesException(left, right, token.Lexeme, token.Line, token.Column);
    }
    
//    private static T BuildException<T>(Token leftToken, Token rightToken) where T : ParserException {
//        try {
//            return (T)Activator.CreateInstance(typeof(T),leftToken, rightToken.Lexeme, rightToken.Line, rightToken.Column);
//        }
//        catch (TargetInvocationException e) {
//            throw e.InnerException;
//        }
//    }

    private static Token GetLeftmostToken(ExprNode node) {
        var type = node.GetType();
        var pi = type.GetProperty("Token");
        
        if (pi != null)
            return (Token) pi.GetValue(node);
        
        pi = type.GetProperty("Operation");
        if (pi != null)
            return (Token) pi.GetValue(node);
        
        // no token found
        
//        pi = type.GetProperty("Left");
//        if (pi ! null)
        throw new ArgumentOutOfRangeException("something really bad happened");
    }
}
}