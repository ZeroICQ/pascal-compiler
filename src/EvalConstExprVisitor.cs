namespace Compiler {
public class EvalConstExprVisitor : IAstVisitor<SymConst> {
    private IdentifierToken _idToken;
    private string Name => _idToken.Value;
    private SymStack _symStack;
    
    public EvalConstExprVisitor(IdentifierToken idToken, SymStack stack) {
        _idToken = idToken;
        _symStack = stack;
    }
    public SymConst Visit(BlockNode node) {
        throw EvalException();
    }

    public SymConst Visit(BinaryExprNode node) {
        var left = node.Left.Accept(this);
        var right = node.Right.Accept(this);

        switch (node.Operation) {
            case OperatorToken op:
                switch (op.Value) {
                    case Constants.Operators.Plus:
                        return EvalPlus(left, right);
                }
                break;
        }

        throw EvalException();
    }

    private SymConst EvalPlus(SymConst left, SymConst right) {
        switch (left) {
            case SymIntConst leftInt:
                switch (right) {
                    case SymIntConst rightInt:
                        return new SymIntConst(_idToken.Value, _symStack.SymInt, leftInt.Value + rightInt.Value);
                }
                break;
            case SymFloatConst leftFloat:
                switch (right) {
                    case SymFloatConst rightFloat:
                        return new SymFloatConst(_idToken.Value, _symStack.SymFloat,leftFloat.Value + rightFloat.Value);
                }
                break;
        }

        throw EvalException();
    }

    public SymConst Visit(IntegerNode node) {
        return new SymIntConst(Name, _symStack.SymInt, node.Token.Value);
    }

    public SymConst Visit(FloatNode node) {
        return new SymFloatConst(Name, _symStack.SymFloat, node.Token.Value);
    }

    public SymConst Visit(IdentifierNode node) {
        // can allow constants
        throw EvalException();
    }

    public SymConst Visit(FunctionCallNode node) {
        throw EvalException();
    }

    public SymConst Visit(CastNode node) {
        throw new System.NotImplementedException();
    }

    public SymConst Visit(AccessNode node) {
        throw EvalException();
    }

    public SymConst Visit(IndexNode node) {
        throw EvalException();
    }

    public SymConst Visit(StringNode node) {
        throw new System.NotImplementedException();
    }

    public SymConst Visit(UnaryOperationNode node) {
        throw new System.NotImplementedException();
    }

    public SymConst Visit(AssignNode node) {
        throw EvalException();
    }

    public SymConst Visit(IfNode node) {
        throw EvalException();
    }

    public SymConst Visit(WhileNode node) {
        throw EvalException();
    }

    public SymConst Visit(ProcedureCallNode node) {
        throw EvalException();
    }

    public SymConst Visit(ForNode node) {
        throw EvalException();
    }

    public SymConst Visit(ControlSequence node) {
        throw EvalException();
    }

    public SymConst Visit(EmptyStatementNode node) {
        throw EvalException();
    }

    public SymConst Visit(CharNode node) {
        throw new System.NotImplementedException();
    }

    private ConstExpressionParsingException EvalException() {
        return new ConstExpressionParsingException(_idToken.Lexeme, _idToken.Line, _idToken.Column);
    }
}
}