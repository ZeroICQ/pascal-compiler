using System.Collections.Generic;
using System.Linq;

namespace Compiler {

public abstract class AstNode {
    public List<AstNode> Children { get; set; } = new List<AstNode>();
    public abstract string StringValue { get; }
    
    public void AppendChild(AstNode child) {
        Children.Add(child);
    }
}

public class RootNode : AstNode {
    public override string StringValue => "[ROOT]";
}

//public class OperatorNode : AstNode {
//    
//}
//
//public class VarRefNode : AstNode {
//    
//}

public class IntegerNode : AstNode {
    public override string StringValue => _value.ToString();
    private readonly long _value;

    public IntegerNode(long value) {
        _value = value;
    }
}

//public StringNode : AstNode {
//
//}

}