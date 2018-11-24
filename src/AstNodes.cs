using System.Collections.Generic;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;

namespace Compiler {

public abstract class AstNode {
    public abstract string StringValue { get; }
    public abstract T Accept<T>(IAstVisitor<T> visitor);
    
}

public abstract class ExprNode : AstNode {

}

public abstract class StmntNode : AstNode {
}

public class BlockNode : StmntNode {
    public override string StringValue => "Block";
    public List<ExprNode> Expressions { get; } = new List<ExprNode>();
    
    public override T Accept<T>(IAstVisitor<T> visitor) {
        return visitor.Visit(this);
    }

    public void AddExpression(ExprNode exprNode) {
        Expressions.Add(exprNode);
    }
}

//--- Expressions ---
public class IdentifierNode : ExprNode {
    public override string StringValue => _token.StringValue;
    
    private IdentifierToken _token;
    
    public override T Accept<T>(IAstVisitor<T> visitor) {
        return visitor.Visit(this);
    }

    public IdentifierNode(IdentifierToken token) {
        _token = token;
    }
}

public abstract class ConstantNode : ExprNode {
    protected Token _token;

    protected ConstantNode(Token token) {
        _token = token;
    }
}

public class IntegerNode : ConstantNode {
    private new IntegerToken _token;
    
    public override string StringValue => _token.StringValue;

    public IntegerNode(IntegerToken token) : base(token) {
        _token = token;
    }

    public override T Accept<T>(IAstVisitor<T> visitor) {
        return visitor.Visit(this);
    }
}

public class BinaryExprNode : ExprNode {
    public AstNode Left { get; }
    public AstNode Right { get; }
    private Token _operator;

    public override string StringValue => _operator.StringValue;
    public override T Accept<T>(IAstVisitor<T> visitor) {
        return visitor.Visit(this);
    }

    public BinaryExprNode(AstNode left, AstNode right, Token @operator) {
        Left = left;
        Right = right;
        _operator = @operator;
    }
}

public class FloatNode : ConstantNode {
    private new FloatToken _token;
    public override string StringValue => _token.StringValue;
    
    public override T Accept<T>(IAstVisitor<T> visitor) {
        return visitor.Visit(this);
    }

    public FloatNode(FloatToken token) : base(token) {
        _token = token;
    }
}


//public StringNode : AstNode {
//
//}

}
