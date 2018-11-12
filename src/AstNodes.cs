using System.Collections.Generic;
using System.Linq;

namespace Compiler {

public abstract class AstNode {
    public List<AstNode> Children { get; set; }
    public abstract string StringValue { get; }
    
    public void AppendChild(AstNode child) {
        Children.Append(child);
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

public class NumberNode : AstNode {
    
    public NumberNode
}

//public StringNode : AstNode {
//
//}

}