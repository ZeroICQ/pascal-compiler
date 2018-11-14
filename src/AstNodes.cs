using System.Collections.Generic;
using System.Net.Sockets;

namespace Compiler {

public abstract class AstNode {
    public abstract string StringValue { get; }
    public abstract void Accept(IAstVisitor visitor);
    
}

public abstract class ExprNode : AstNode {
//    protected ExprNode(Token token) : base(token) {}
    // type
}

public abstract class StmntNode : AstNode {
}

public class BlockNode : StmntNode {
    public override string StringValue => "Block";
    private List<ExprNode> _expressions = new List<ExprNode>();
    
    public override void Accept(IAstVisitor visitor) {
        visitor.Visit(this);
    }

    public void AddExpression(ExprNode exprNode) {
        _expressions.Add(exprNode);
    }
}

public class BinaryExprNode : ExprNode {
    public AstNode Left { get; }
    public AstNode Right { get; }
    private Token _operator;

    public override string StringValue => _operator.StringValue;
    public override void Accept(IAstVisitor visitor) {
        visitor.Visit(this);
    }

    public BinaryExprNode(AstNode left, AstNode right, Token @operator) {
        Left = left;
        Right = right;
        _operator = @operator;
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
    public override void Accept(IAstVisitor visitor) {
        visitor.Visit(this);
    }

    public IntegerNode(IntegerToken token) : base(token) {
        _token = token;
    }
}

//public class VarRefNode : AstNode {
//    
//}


//public StringNode : AstNode {
//
//}

}
