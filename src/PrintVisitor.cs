using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Compiler {
public class PrintVisitor : IAstVisitor<PrinterNode> {
    
    public PrinterNode Visit(BlockNode node) {
        var pNode = new PrinterNode(node.StringValue);

        foreach (var expr in node.Expressions) {
            var child = expr.Accept(this);
            pNode.AddChild(child);
        }

        return pNode;
    }

    public PrinterNode Visit(BinaryExprNode node) {
        var pNode = new PrinterNode(node.StringValue);
        pNode.AddChild(node.Left.Accept(this));
        pNode.AddChild(node.Right.Accept(this));
        return pNode;
    }

    public PrinterNode Visit(IntegerNode node) {
        return new PrinterNode(node.StringValue);
    }
}

public class PrinterNode {
    public int Width {
        get { return Math.Max(Value.Length, ChildrenWidth() + (_children.Count > 1 ? (_children.Count - 1) * 2 : 0)); }
    }
    public string Value { get; }
    private List<PrinterNode> _children = new List<PrinterNode>();

    public PrinterNode(string value) {
        Value = value;
    }

    public void AddChild(PrinterNode child) {
        _children.Add(child);
    }

    public void Print(bool IsEndl = true, int offset = 0, bool IsPrintSpace = false) {
        var leftPadding = (Width - Value.Length ) / 2;
        // global
        Console.Write(new string(' ', offset));
        // local
        Console.Write(new string(' ', leftPadding));
        
        Console.Write(Value);
        if (IsPrintSpace)
            Console.Write("  ");
        
        if (IsEndl)
            Console.WriteLine();
        
        for (var i = 0; i < _children.Count; i++) {
            var space = i <= _children.Count - 2;
            _children[i].Print(i == _children.Count - 1, offset, space);
            offset = _children[i].Width + (space ? 2 : 0);
        }
    }

    private int ChildrenWidth() {
        var childrenWidth = 0;
        foreach (var c in _children) {
            childrenWidth += c.Width;
        }
        return childrenWidth; 
    }
    
}

}