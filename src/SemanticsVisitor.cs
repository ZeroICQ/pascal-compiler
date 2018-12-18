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
        
         
        _typeChecker.RequireBinary(ref node.Left, ref node.Right, node.Operation);
        
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
        var sym = _stack.FindVar(node.Token.Value);
        if (sym == null)
            throw BuildException<IdentifierNotDefinedException>(node.Token);
        
        node.Symbol = sym;
        node.Type = sym.Type;
        node.IsLvalue = true;
        
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
//        if (node.Left.Type == null)
//            node.Left.Accept(this);
//        
//        if (node.Right.Type == null)
//            node.Right.Accept(this);
        
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

    public bool Visit(ControlSequence node) {
        throw new System.NotImplementedException();
    }

    public bool Visit(EmptyStatementNode node) {
        throw new System.NotImplementedException();
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
    
}
}