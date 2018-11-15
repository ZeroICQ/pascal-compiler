using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    public int Width => Math.Max(Value.Length, ChildrenWidth() + (_children.Count > 1 ? (_children.Count - 1) * 2 : 0));
    public string Value { get; }
    private List<PrinterNode> _children = new List<PrinterNode>();

    public PrinterNode(string value) {
        Value = value;
    }

    public void AddChild(PrinterNode child) {
        _children.Add(child);
    }

    public void Print(in List<StringBuilder> canvas, int offset = 0, int depth = 0, bool space = false) {
        if (canvas.Count - 1 < depth) {
            canvas.Add(new StringBuilder());
        }
        
        var leftPadding = (Width - Value.Length ) / 2;

        if (canvas[depth].Length < offset + leftPadding) {
            var lastIndex = canvas[depth].Length;
            var needInsert = offset + leftPadding - canvas[depth].Length;
            canvas[depth].Insert(lastIndex, " ", needInsert);
        }

        canvas[depth].Insert(offset + leftPadding, Value);
        if (space)
            canvas[depth].Insert(canvas[depth].Length, "  ");
        
        for (var i = 0; i < _children.Count; i++) {
            var isNeedSpace = i <= _children.Count - 2;
            _children[i].Print(canvas, offset, depth + 1, isNeedSpace);
            offset += _children[i].Width + (isNeedSpace ? 2 : 0);
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