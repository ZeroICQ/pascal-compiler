using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Compiler {

public abstract class AstNode {
    public abstract T Accept<T>(IAstVisitor<T> visitor);
    
}

public abstract class ExprNode : AstNode {
    public virtual SymType Type { get; set; }
    public bool IsLvalue { get; set; } = false;
    
    public static Token GetClosestToken(ExprNode node) {
        var type = node.GetType();
        var pi = type.GetProperty(nameof(IdentifierNode.Token));
            
        if (pi != null)
            return (Token) pi.GetValue(node);
            
        pi = type.GetProperty(nameof(BinaryExprNode.Operation));
        if (pi != null)
            return (Token) pi.GetValue(node);
        
        pi = type.GetProperty(nameof(CastNode.Expr));
        if (pi != null)
            return GetClosestToken((ExprNode) pi.GetValue(node));
        
        
        pi = type.GetProperty(nameof(IndexNode.Operand));
        if (pi != null)
            return GetClosestToken((ExprNode) pi.GetValue(node));
        
        pi = type.GetProperty(nameof(AccessNode.Name));
        if (pi != null)
            return GetClosestToken((ExprNode) pi.GetValue(node));
            
        // no token found
        //        pi = type.GetProperty("Left");
        //        if (pi ! null)
        throw new ArgumentOutOfRangeException("Token was not found in GetClosest token method");
    }

}

//--- Expressions ---
public class IdentifierNode : ExprNode {
    public IdentifierToken Token { get; }
    //identifier can be variable, const, or function 
    public SymVarOrConst Symbol { get; set; }
        
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

public class DoubleNode : ConstantNode {
    public DoubleToken Token { get; }
    
    public override T Accept<T>(IAstVisitor<T> visitor) {
        return visitor.Visit(this);
    }

    public DoubleNode(DoubleToken token) : base(token) {
        Token = token;
    }
}

public class StringNode : ConstantNode {
    public StringToken Token { get; }

    public StringNode(StringToken token) : base(token) {
        Token = token;
    }
        
    public override T Accept<T>(IAstVisitor<T> visitor) {
        return visitor.Visit(this);
    }
}

public class CharNode : ConstantNode {
    public StringToken Token { get; }
    public char Value => Token.Value[0];
    public CharNode(StringToken token) : base(token) {
        Token = token;
    }
        
    public override T Accept<T>(IAstVisitor<T> visitor) {
        return visitor.Visit(this);
    }
}

public class UnaryOperationNode : ExprNode {
    public Token Operation { get; }
    public ExprNode Expr { get; }

    public UnaryOperationNode(Token operation, ExprNode expr) {
        Operation = operation;
        Expr = expr;
    }
    
    public override T Accept<T>(IAstVisitor<T> visitor) {
        return visitor.Visit(this);
    }
}

public class BinaryExprNode : ExprNode {
    public ExprNode Left;
    public ExprNode Right;
    public Token Operation { get; }

    public BinaryExprNode(ExprNode left, ExprNode right, Token operation) {
        Left = left;
        Right = right;
        Operation = operation;
    }

    public override T Accept<T>(IAstVisitor<T> visitor) {
        return visitor.Visit(this);
    }
}

public class FunctionCallNode : ExprNode {
    public ExprNode Name { get; }
    public List<ExprNode> Args { get; } 
    public SymFunc Symbol { get; set; }
    
    public FunctionCallNode(ExprNode name, List<ExprNode> args) {
        Name = name;
        Args = args;
    }
    
    public override T Accept<T>(IAstVisitor<T> visitor) {
        return visitor.Visit(this);
    }
}

public class CastNode : ExprNode {
    public ExprNode Expr { get; }
    public SymType CastTo { get; }

    public CastNode(SymType type, ExprNode expr) {
        CastTo = type;
        Expr = expr;
    }
    
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
    public ExprNode IndexExpr;

    public IndexNode(ExprNode operand, ExprNode indexExpr) {
        Operand = operand;
        IndexExpr = indexExpr;
    }
    
    public override T Accept<T>(IAstVisitor<T> visitor) {
        return visitor.Visit(this);
    }
}

// statements
public abstract class StatementNode : AstNode {
}

public class WritelnStatementNode : StatementNode {
    public List<ExprNode> Args { get; } 
    
    public WritelnStatementNode(List<ExprNode> args) {
        Args = args;
    }
    
    public override T Accept<T>(IAstVisitor<T> visitor) {
        return visitor.Visit(this);
    }
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
    public ExprNode Left;
    public ExprNode Right;
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
    public ExprNode Condition;
    // nullable
    public StatementNode TrueBranch { get; }
    // nullable
    public StatementNode FalseBranch { get; }

    public IfNode(ExprNode condition, StatementNode trueBranch = null, StatementNode falseBranch = null) {
        Condition = condition;
        TrueBranch = trueBranch;
        FalseBranch = falseBranch;
    }
    
    public override T Accept<T>(IAstVisitor<T> visitor) {
        return visitor.Visit(this);
    }
}

public class WhileNode : StatementNode {
    public ExprNode Condition;
    public StatementNode Block { get; }

    public WhileNode(ExprNode condition, StatementNode block) {
        Condition = condition;
        Block = block;
    }
    
    public override T Accept<T>(IAstVisitor<T> visitor) {
        return visitor.Visit(this);
    }
}

public class ProcedureCallNode : StatementNode {
    public FunctionCallNode Function;
    
    public ProcedureCallNode(FunctionCallNode function) {
        Function = function;
    }
    
    public override T Accept<T>(IAstVisitor<T> visitor) {
        return visitor.Visit(this);
    }
}

public class ForNode : StatementNode {
    public enum DirectionType {To, Downto}

    public AssignNode Initial { get; }
    public DirectionType Direction { get; }
    public ExprNode Final;
    public StatementNode Body { get; }

    public ForNode(AssignNode initial, DirectionType direction, ExprNode final, StatementNode body) {
        Initial = initial;
        Direction = direction;
        Final = final;
        Body = body;
    }
    
    public override T Accept<T>(IAstVisitor<T> visitor) {
        return visitor.Visit(this);
    }
}

public class ControlSequence : StatementNode {
    public ReservedToken ControlWord { get; }

    public ControlSequence(ReservedToken word) {
        ControlWord = word;
    }
    public override T Accept<T>(IAstVisitor<T> visitor) {
        return visitor.Visit(this);
    }
}

public class EmptyStatementNode : StatementNode {
    public override T Accept<T>(IAstVisitor<T> visitor) {
        return visitor.Visit(this);
    }
    
}


// TODO: return;

}
