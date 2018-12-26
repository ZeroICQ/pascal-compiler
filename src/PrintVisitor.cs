using System;
using System.Collections.Generic;
using System.Text;

namespace Compiler {
public class PrintVisitor : IAstVisitor<PrinterNode> {
    public PrinterNode Visit(BlockNode node) {
        var blockTitle = node.IsMain ? "Main Block" : "Block";
        var pNode = new PrinterNode(blockTitle);

        foreach (var expr in node.Statements) {
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

    public PrinterNode Visit(StringNode node) {
        return new PrinterNode(node.Token.StringValue);
    }

    public PrinterNode Visit(IdentifierNode node) {
        return new PrinterNode(node.Token.StringValue);
    }

    public PrinterNode Visit(FunctionCallNode node) {
        var pNode = new PrinterNode("Function");
        pNode.AddChild(node.Name.Accept(this));
        foreach (var arg in node.Args) {
            pNode.AddChild(arg.Accept(this));
        }
        return pNode;
    }

    public PrinterNode Visit(CastNode node) {
        var pNode = new PrinterNode($"({node.Type.Name})");
        pNode.AddChild(node.Expr.Accept(this));
        return pNode;
    }

    public PrinterNode Visit(AccessNode node) {
        var pNode = new PrinterNode("Access");
        pNode.AddChild(node.Name.Accept(this));
        pNode.AddChild(new PrinterNode(node.Field.StringValue));
        return pNode;
    }

    public PrinterNode Visit(IndexNode node) {
        var pNode = new PrinterNode("Index");
        pNode.AddChild(node.Operand.Accept(this));
        pNode.AddChild(node.IndexExpr.Accept(this));
        return pNode;
    }

    public PrinterNode Visit(UnaryOperationNode node) {
        var pNode = new PrinterNode(node.Operation.StringValue);
        pNode.AddChild(node.Expr.Accept(this));
        return pNode;
    }
    
    public PrinterNode Visit(AssignNode node) {
        var pNode = new PrinterNode(node.Operation.StringValue);
        pNode.AddChild(node.Left.Accept(this));
        pNode.AddChild(node.Right.Accept(this));
        return pNode;
    }

    public PrinterNode Visit(IfNode node) {
        var pNode = new PrinterNode("If");
        pNode.AddChild(node.Condition.Accept(this));
        
        if (node.TrueBranch != null)
            pNode.AddChild(node.TrueBranch.Accept(this));
        
        if (node.FalseBranch != null)
            pNode.AddChild(node.FalseBranch.Accept(this));
        
        return pNode;
    }

    public PrinterNode Visit(WhileNode node) {
        var pNode = new PrinterNode("While");
        pNode.AddChild(node.Condition.Accept(this));
        pNode.AddChild(node.Block.Accept(this));
        return pNode;
    }

    public PrinterNode Visit(ProcedureCallNode node) {
        var pNode = new PrinterNode("Procedure");
        
        pNode.AddChild(node.Function.Name.Accept(this));
        foreach (var arg in node.Function.Args) {
            pNode.AddChild(arg.Accept(this));
        }
        return pNode;
    }

    public PrinterNode Visit(ForNode node) {
        var pNode = new PrinterNode($"For {node.Direction.ToString()}");
        pNode.AddChild(node.Initial.Accept(this));
        pNode.AddChild(node.Final.Accept(this));
        pNode.AddChild(node.Body.Accept(this));
        return pNode;
    }
    
    public PrinterNode Visit(ControlSequence node) {
        var pNode = new PrinterNode(node.ControlWord.StringValue);
        return pNode;
    }

    public PrinterNode Visit(EmptyStatementNode node) {
        return new PrinterNode("Empty statement");
    }

    public PrinterNode Visit(CharNode node) {
        return new PrinterNode(node.Token.StringValue);
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
            
            PrintEdge(canvas, depth + 1, start, end, i == 0, i == _children.Count - 1);
            
            _children[i].Print(canvas, offset, depth + 2, isNeedSpace);
            offset += _children[i].Width + (isNeedSpace ? 2 : 0);
        }
    }

    private void PrintEdge(in List<StringBuilder> canvas, int depth, int start, int end, bool isLeftEdge, bool isRightEdge) {
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
            if (curLine[i] == ' ')
                curLine[i] = Convert.ToChar(0x2500);
        }
        

        curLine[start] = '┴';
        if (rightmost > start) {
            if (isRightEdge)
                curLine[rightmost] = '┐';
            else 
                curLine[rightmost] = '┬';
        }
        else if (rightmost > end) {
            if (isLeftEdge)
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
