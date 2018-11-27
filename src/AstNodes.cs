using System.Collections.Generic;

namespace Compiler {

public abstract class AstNode {
    public abstract T Accept<T>(IAstVisitor<T> visitor);
    
}

public abstract class ExprNode : AstNode {

}


//--- Expressions ---
public class IdentifierNode : ExprNode {
    public IdentifierToken Token { get; }
    
    public override T Accept<T>(IAstVisitor<T> visitor) {
        return visitor.Visit(this);
    }

    public IdentifierNode(IdentifierToken token) {
        Token = token;
    }
}

public abstract class ConstantNode : ExprNode {
    protected Token _token;

    protected ConstantNode(Token token) {
        _token = token;
    }
}

public class IntegerNode : ConstantNode {
    public IntegerToken Token { get; }

    public IntegerNode(IntegerToken token) : base(token) {
        Token = token;
    }

    public override T Accept<T>(IAstVisitor<T> visitor) {
        return visitor.Visit(this);
    }
}

public class FloatNode : ConstantNode {
    public FloatToken Token;
    
    public override T Accept<T>(IAstVisitor<T> visitor) {
        return visitor.Visit(this);
    }

    public FloatNode(FloatToken token) : base(token) {
        Token = token;
    }
}

public class CharNode : ConstantNode {
    public StringToken Token { get; }

    public CharNode(StringToken token) : base(token) {
        Token = token;
    }
        
    public override T Accept<T>(IAstVisitor<T> visitor) {
        return visitor.Visit(this);
    }
}

public class UnaryOperationNode : ExprNode {
    public Token Operation { get; }
    public ExprNode Operand { get; }

    public UnaryOperationNode(Token operation, ExprNode operand) {
        Operation = operation;
        Operand = operand;
    }
    
    public override T Accept<T>(IAstVisitor<T> visitor) {
        return visitor.Visit(this);
    }
}

public class BinaryExprNode : ExprNode {
    public AstNode Left { get; }
    public AstNode Right { get; }
    public Token Operation { get; }

    public BinaryExprNode(AstNode left, AstNode right, Token operation) {
        Left = left;
        Right = right;
        Operation = operation;
    }

    public override T Accept<T>(IAstVisitor<T> visitor) {
        return visitor.Visit(this);
    }
}

public class FunctionCallNode : ExprNode {
    public IdentifierToken Name { get; }
    public List<ExprNode> Args { get; } 
    
    public FunctionCallNode(IdentifierToken name, List<ExprNode> args) {
        Name = name;
        Args = args;
    }
    
    public override T Accept<T>(IAstVisitor<T> visitor) {
        return visitor.Visit(this);
    }
}

public class CastNode : UnaryOperationNode {
    public CastNode(IdentifierToken name, ExprNode operand) : base(name, operand) {}
    
    public override T Accept<T>(IAstVisitor<T> visitor) {
        return visitor.Visit(this);
    }
}

public class AccessNode : ExprNode {
    public ExprNode Name { get; }
    public IdentifierToken Field { get; }

    public AccessNode(ExprNode name, IdentifierToken field) {
        Name = name;
        Field = field;
    }
    
    public override T Accept<T>(IAstVisitor<T> visitor) {
        return visitor.Visit(this);
    }
}

// a[3]
public class IndexNode : ExprNode {
    public ExprNode Operand { get; }
    public ExprNode Index { get; }

    public IndexNode(ExprNode operand, ExprNode index) {
        Operand = operand;
        Index = index;
    }
    
    public override T Accept<T>(IAstVisitor<T> visitor) {
        return visitor.Visit(this);
    }
}

// statements
public abstract class StatementNode : AstNode {
}

public class BlockNode : StatementNode {
    public List<StatementNode> Statements { get; } = new List<StatementNode>();
    public bool IsMain { get; set; } = false;
    
    public override T Accept<T>(IAstVisitor<T> visitor) {
        return visitor.Visit(this);
    }

    public void AddStatement(StatementNode statementNode) {
        Statements.Add(statementNode);
    }
}

public class AssignNode : StatementNode {
    public ExprNode Left { get; }
    public ExprNode Right { get; }
    public OperatorToken Operation { get; }
    
    public AssignNode(ExprNode left, OperatorToken op, ExprNode right) {
        Left = left;
        Right = right;
        Operation = op;
    }
    
    public override T Accept<T>(IAstVisitor<T> visitor) {
        return visitor.Visit(this);
    }
    
}

public class IfNode : StatementNode {
    public ExprNode Condition { get; }
    public BlockNode TrueBranch { get; }
    public BlockNode FalseBranch { get; }

    public IfNode(ExprNode condition, BlockNode trueBranch, BlockNode falseBranch) {
        Condition = condition;
        TrueBranch = trueBranch;
        FalseBranch = falseBranch;
    }
    
    public override T Accept<T>(IAstVisitor<T> visitor) {
        return visitor.Visit(this);
    }
}

public class WhileNode : StatementNode {
    public ExprNode Condition { get; }
    public BlockNode Block { get; }

    public WhileNode(ExprNode condition, BlockNode block) {
        Condition = condition;
        Block = block;
    }
    
    public override T Accept<T>(IAstVisitor<T> visitor) {
        return visitor.Visit(this);
    }
}


public class ProcedureCallNode : StatementNode {
    public ExprNode Name { get; }
    public List<ExprNode> Args { get; } 
    
    public ProcedureCallNode(ExprNode name, List<ExprNode> args) {
        Name = name;
        Args = args;
    }
    
    public override T Accept<T>(IAstVisitor<T> visitor) {
        return visitor.Visit(this);
    }
}


// TODO: for, continue, break, return;

}
