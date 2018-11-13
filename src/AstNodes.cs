using System.Collections.Generic;

namespace Compiler {

public abstract class AstNode {
    public abstract string StringValue { get; }
    public abstract void Accept(IAstVisitor visitor);
}

public abstract class MultiChildrenNode : AstNode {
    public List<AstNode> Children { get; } = new List<AstNode>();

    public void AppendChild(AstNode child) {
        Children.Add(child);
    }

    public override void Accept(IAstVisitor visitor) {
        visitor.Visit(this);
    }
}

public abstract class BinaryNode : AstNode {
    public AstNode Left { get; }
    public AstNode Right { get; }

    protected BinaryNode(AstNode left, AstNode right) {
        Left = left;
        Right = right;
    }
}

public class RootNode : MultiChildrenNode {
    public override string StringValue => "[ROOT]";
}

public abstract class ConstantNode : AstNode {}

public class IntegerNode : ConstantNode {
    public override string StringValue => _value.ToString();
    private readonly long _value;
    
    public IntegerNode(long value) {
        _value = value;
    }

    public override void Accept(IAstVisitor visitor) {
        visitor.Visit(this);
    }
}


// OPERATORS
public class LessNode : BinaryNode {
    public override void Accept(IAstVisitor visitor) {
        visitor.Visit(this);
    }

    public override string StringValue => Symbols.Operators.Less.ToString();
    public LessNode(AstNode left, AstNode right) : base(left, right) {}
}

public class LessOrEqualNode : BinaryNode {
    public override void Accept(IAstVisitor visitor) {
        visitor.Visit(this);
    }

    public override string StringValue => Symbols.Operators.LessOrEqual.ToString();
    public LessOrEqualNode(AstNode left, AstNode right) : base(left, right) { }
}


public class MoreNode : BinaryNode {
    public override void Accept(IAstVisitor visitor) {
        visitor.Visit(this);
    }

    public override string StringValue => Symbols.Operators.More.ToString();
    public MoreNode(AstNode left, AstNode right) : base(left, right) { }
}

public class MoreOrEqual : BinaryNode {
    public override void Accept(IAstVisitor visitor) {
        throw new System.NotImplementedException();
    }

    public override string StringValue => Symbols.Operators.MoreOreEqual.ToString();
    public MoreOrEqual(AstNode left, AstNode right) : base(left, right) { }
}

public class EqualNode : BinaryNode {
    public override void Accept(IAstVisitor visitor) {
        visitor.Visit(this);
    }

    public override string StringValue => Symbols.Operators.Equal.ToString();
    public EqualNode(AstNode left, AstNode right) : base(left, right) { }
}

public class NotEqualNode : BinaryNode {
    public override void Accept(IAstVisitor visitor) {
        visitor.Visit(this);
    }

    public override string StringValue => Symbols.Operators.NotEqual.ToString();
    public NotEqualNode(AstNode left, AstNode right) : base(left, right) { }
}

public class InNode : BinaryNode {
    public override void Accept(IAstVisitor visitor) {
        visitor.Visit(this);
    }

    public override string StringValue => Symbols.Words.In.ToString();
    public InNode(AstNode left, AstNode right) : base(left, right) { }

}


//public class VarRefNode : AstNode {
//    
//}


//public StringNode : AstNode {
//
//}

}
