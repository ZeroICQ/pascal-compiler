using System;
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
        if (node.Left.Type == null)
            node.Left.Accept(this);

        if (node.Right.Type == null)
            node.Right.Accept(this);
        
        // todo add switch check for all operation + casts
        if (!ReferenceEquals(node.Left.Type, node.Right.Type))
            throw Incompatible(node.Left.Type, node.Right.Type, node.Right);
        
        // todo rmk to match cast
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
        // todo: assign can also be +=, -= etc... 
        EnsureTypesMatch(ref node.Left, ref node.Right);

        if (!node.Left.IsLvalue)
            throw BuildException<NotLvalueException>(GetClosestToken(node.Left));
        
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

    private void EnsureTypesMatch(ref ExprNode left, ref ExprNode right) {
        if (!MatchTypes(ref left, ref right))        
            throw Incompatible(left.Type, right.Type, right);
    }

    private bool MatchTypes(ref ExprNode left, ref ExprNode right) {
        switch (left.Type) {
            // scalars
            // float
            case SymFloat leftFloat:
                switch (right.Type) {
                    case SymInt _:
                        right = new CastNode(new IdentifierToken(_stack.SymFloat.Name,0, 0), right);
                        return true;
                    
                    case SymFloat _:
                        return true;
                }
                break;
            // end float
            // int
            case SymInt leftInt:
                switch (right.Type) {
                    case SymInt _:
                        return true;
                }
            break;
            // end int
        }

        return false;
    }

    private static IncompatibleTypesException Incompatible(SymType left, SymType right, ExprNode node) {
        var token = GetClosestToken(node);
        return new IncompatibleTypesException(left, right, token.Lexeme, token.Line, token.Column);
    }

    // extract some token from exprnode 
    private static Token GetClosestToken(ExprNode node) {
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