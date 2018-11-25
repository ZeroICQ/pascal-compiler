using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Compiler {
public class PrintVisitor : IAstVisitor<PrinterNode> {
    public PrinterNode Visit(BlockNode node) {
        var pNode = new PrinterNode("Block");

        foreach (var expr in node.Expressions) {
            var child = expr.Accept(this);
            pNode.AddChild(child);
        }

        return pNode;
    }

    public PrinterNode Visit(BinaryExprNode node) {
        var pNode = new PrinterNode(node.Operation.StringValue);
        pNode.AddChild(node.Left.Accept(this));
        pNode.AddChild(node.Right.Accept(this));
        return pNode;
    }

    public PrinterNode Visit(IntegerNode node) {
        return new PrinterNode(node.Token.StringValue);
    }

    public PrinterNode Visit(FloatNode node) {
        return new PrinterNode(node.Token.StringValue);
    }

    public PrinterNode Visit(CharNode node) {
        return new PrinterNode(node.Token.StringValue);
    }

    public PrinterNode Visit(IdentifierNode node) {
        return new PrinterNode(node.Token.StringValue);
    }

    public PrinterNode Visit(FunctionCallNode node) {
        var pNode = new PrinterNode(node.Name.StringValue);
        foreach (var arg in node.Args) {
            pNode.AddChild(arg.Accept(this));
        }
        return pNode;
    }

    public PrinterNode Visit(CastNode node) {
        return Visit((UnaryOperationNode) node);
    }

    public PrinterNode Visit(AccessNode node) {
        var pNode = node.Name.Accept(this);
        pNode.AddChild(new PrinterNode(node.Field.StringValue));
        return pNode;
    }

    public PrinterNode Visit(IndexNode node) {
        var pNode = node.Operand.Accept(this);
        pNode.AddChild(node.Index.Accept(this));
        return pNode;
    }

    public PrinterNode Visit(UnaryOperationNode node) {
        var pNode = new PrinterNode(node.Operation.StringValue);
        pNode.AddChild(node.Operand.Accept(this));
        return pNode;
    }
    
    public PrinterNode Visit(AssignNode node) {
        var pNode = new PrinterNode(":=");
        pNode.AddChild(node.Left.Accept(this));
        pNode.AddChild(node.Right.Accept(this));
        return pNode;
    }

    public PrinterNode Visit(IfNode node) {
        var pNode = new PrinterNode("if");
        pNode.AddChild(node.Condition.Accept(this));
        pNode.AddChild(node.TrueBranch.Accept(this));
        pNode.AddChild(node.FalseBranch.Accept(this));
        return pNode;
    }

    public PrinterNode Visit(WhileNode node) {
        var pNode = new PrinterNode("While");
        pNode.AddChild(node.Condition.Accept(this));
        pNode.AddChild(node.Block.Accept(this));
        return pNode;
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
        
        var start = Width / 2 + offset;
        for (var i = 0; i < _children.Count; i++) {
            var isNeedSpace = i <= _children.Count - 2;            
            var end = offset + _children[i].Width / 2;
            
            PrintEdge(canvas, depth + 1, start, end, i == 0 || i == _children.Count - 1);
            
            _children[i].Print(canvas, offset, depth + 2, isNeedSpace);
            offset += _children[i].Width + (isNeedSpace ? 2 : 0);
        }
    }

    private void PrintEdge(in List<StringBuilder> canvas, int depth, int start, int end, bool isEdge) {
        Console.OutputEncoding = Encoding.UTF8;
        
        if (canvas.Count - 1 < depth) {
            canvas.Add(new StringBuilder());
        }

        var curLine = canvas[depth];
        var leftmost = Math.Min(start, end);
        var rightmost = Math.Max(start, end);

        if (rightmost > curLine.Length - 1) {
            curLine.Insert(curLine.Length, " ", rightmost - curLine.Length + 1);
        }

        for (int i = leftmost; i <= rightmost; i++) {
            curLine[i] = Convert.ToChar(0x2500);
        }
        

        curLine[start] = '┴';
        if (rightmost > start) {
            if (isEdge)
                curLine[rightmost] = '┐';
            else 
                curLine[rightmost] = '┬';
        }
        else if (rightmost > end) {
            if (isEdge)
                curLine[leftmost] = '┌';
            else 
                curLine[leftmost] = '┬';
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
