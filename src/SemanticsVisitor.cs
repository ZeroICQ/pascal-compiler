namespace Compiler {

// sets Type values, makes implicit typecasts, 
public class SemanticsVisitor : IAstVisitor<bool> {
    private readonly SymStack _stack;

    public SemanticsVisitor(SymStack stack) {
        _stack = stack;
    }
    
    public bool Visit(BlockNode node) {
        foreach (var statement in node.Statements) {
            statement.Accept(this);
        }

        return true;
    }

    public bool Visit(BinaryExprNode node) {
        throw new System.NotImplementedException();
    }

    public bool Visit(IntegerNode node) {
        throw new System.NotImplementedException();
    }

    public bool Visit(FloatNode node) {
        throw new System.NotImplementedException();
    }

    public bool Visit(IdentifierNode node) {
        throw new System.NotImplementedException();
    }

    public bool Visit(FunctionCallNode node) {
        throw new System.NotImplementedException();
    }

    public bool Visit(CastNode node) {
        throw new System.NotImplementedException();
    }

    public bool Visit(AccessNode node) {
        throw new System.NotImplementedException();
    }

    public bool Visit(IndexNode node) {
        throw new System.NotImplementedException();
    }

    public bool Visit(StringNode node) {
        throw new System.NotImplementedException();
    }

    public bool Visit(UnaryOperationNode node) {
        throw new System.NotImplementedException();
    }

    public bool Visit(AssignNode node) {
        throw new System.NotImplementedException();
    }

    public bool Visit(IfNode node) {
        throw new System.NotImplementedException();
    }

    public bool Visit(WhileNode node) {
        throw new System.NotImplementedException();
    }

    public bool Visit(ProcedureCallNode node) {
        throw new System.NotImplementedException();
    }

    public bool Visit(ForNode node) {
        throw new System.NotImplementedException();
    }

    public bool Visit(ControlSequence node) {
        throw new System.NotImplementedException();
    }

    public bool Visit(EmptyStatementNode node) {
        throw new System.NotImplementedException();
    }

    public bool Visit(CharNode node) {
        throw new System.NotImplementedException();
    }
}
}