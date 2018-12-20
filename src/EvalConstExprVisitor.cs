namespace Compiler {
public class EvalConstExprVisitor : IAstVisitor<SymConst> {
    private IdentifierToken _idToken;
    private Token _token;
    private string Name => _idToken?.Value;
    private SymStack _symStack;
    
    public EvalConstExprVisitor(IdentifierToken idToken, SymStack stack) {
        _idToken = idToken;
        _symStack = stack;
    }
    
    public EvalConstExprVisitor(Token token, SymStack stack) {
        _token = token;
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
                    case Constants.Operators.Minus:
                        return EvalMinus(left, right);
                    case Constants.Operators.Multiply:
                        return EvalMultiply(left, right);
                    case Constants.Operators.Divide:
                        return EvalDivide(left, right); 
                }
                break;
        }

        throw EvalException();
    }
    
    private SymConst EvalDivide(SymConst left, SymConst right) {
        switch (left) {
            case SymIntConst leftInt:
                switch (right) {
                    case SymIntConst rightInt:
                        return new SymFloatConst(Name, _symStack.SymFloat, (double) leftInt.Value / rightInt.Value);
                }
                break;
            case SymFloatConst leftFloat:
                switch (right) {
                    case SymFloatConst rightFloat:
                        return new SymFloatConst(Name, _symStack.SymFloat,leftFloat.Value / rightFloat.Value);
                }
                break;
        }

        throw EvalException();
    }

    private SymConst EvalMultiply(SymConst left, SymConst right) {
        switch (left) {
            case SymIntConst leftInt:
                switch (right) {
                    case SymIntConst rightInt:
                        return new SymIntConst(Name, _symStack.SymInt, leftInt.Value * rightInt.Value);
                }
                break;
            case SymFloatConst leftFloat:
                switch (right) {
                    case SymFloatConst rightFloat:
                        return new SymFloatConst(Name, _symStack.SymFloat,leftFloat.Value * rightFloat.Value);
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
                        return new SymIntConst(Name, _symStack.SymInt, leftInt.Value + rightInt.Value);
                }
                break;
            case SymFloatConst leftFloat:
                switch (right) {
                    case SymFloatConst rightFloat:
                        return new SymFloatConst(Name, _symStack.SymFloat,leftFloat.Value + rightFloat.Value);
                }
                break;
        }

        throw EvalException();
    }
    
    private SymConst EvalMinus(SymConst left, SymConst right) {
        switch (left) {
            case SymIntConst leftInt:
                switch (right) {
                    case SymIntConst rightInt:
                        return new SymIntConst(Name, _symStack.SymInt, leftInt.Value - rightInt.Value);
                }
                break;
            case SymFloatConst leftFloat:
                switch (right) {
                    case SymFloatConst rightFloat:
                        return new SymFloatConst(Name, _symStack.SymFloat,leftFloat.Value - rightFloat.Value);
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

    public SymConst Visit(CharNode node) {
        return new SymCharConst(Name, _symStack.SymChar, node.Value);
    }

    public SymConst Visit(IdentifierNode node) {
        // can allow constants
        throw EvalException();
    }

    public SymConst Visit(FunctionCallNode node) {
        throw EvalException();
    }

    public SymConst Visit(CastNode node) {
        var castingConst = node.Expr.Accept(this);
        // Explicit casts are currently not allowed in const expr. The only available implicit cast is int -> float.
        switch (node.Type) {
            case SymFloat s:
                switch (castingConst) {
                    case SymIntConst intConst:
                        return new SymFloatConst(intConst.Name, _symStack.SymFloat, intConst.Value);
                    case SymFloatConst floatConst:
                        return castingConst;
                }
                break;
        }
        throw EvalException();
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
        // todo: maybe implement other unary operations
        var exprConst = node.Expr.Accept(this);
        switch (node.Operation) {
            
            case OperatorToken operatorToken:
                
                switch (operatorToken.Value) {
                    
                    case Constants.Operators.Minus:
                        
                        switch (exprConst) {
                            
                            case SymIntConst intConst:
                                return new SymIntConst(intConst.Name, intConst.Type, -intConst.Value);
                            case SymFloatConst floatConst:
                                return new SymFloatConst(floatConst.Name, floatConst.Type, -floatConst.Value);
                        }
                        break;
                }
                break;
        }
        throw EvalException();
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

    private ConstExprEvalException EvalException() {
        if (_idToken == null) {
            return new AnonConstExprEvalException(_token.Lexeme, _token.Line, _token.Column);
        }
        return new NamedConstExprEvalException(_idToken.Lexeme, _idToken.Line, _idToken.Column);
    }
}
}