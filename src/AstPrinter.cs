using System;

namespace Compiler {
public class AstPrinter : IAstVisitor {
    private int _currNodeNumber = -1;
    private int _currParentNode = 0;
    
    public delegate void accept(IAstVisitor visitor);
    
    public void Visit(MultiChildrenNode node) {
        AdvanceNodeNumber(node);
        var myNumber = _currNodeNumber;
        
        foreach (var child in node.Children) {
            AcceptNext(child.Accept, myNumber);        
        }
    }

    public void Visit(IntegerNode node) {
        AdvanceNodeNumber(node);
    }

    private void AdvanceNodeNumber(AstNode node) {
        _currNodeNumber += 1;
        Console.WriteLine($"{_currNodeNumber},{_currParentNode}\t {node.StringValue}");
        _currParentNode = _currNodeNumber;
    }

    private void AcceptNext(accept f, int parentNodeNumber) {
        f(this);
        _currParentNode = parentNodeNumber;
    }
}
}

