using System.Collections.Generic;
using System.Linq;

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

//public class VarRefNode : AstNode {
//    
//}


//public StringNode : AstNode {
//
//}

}
