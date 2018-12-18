using System;
using System.Collections;

namespace Compiler {
public class TypeChecker {
    private readonly SymStack _stack;

    public TypeChecker(SymStack stack) {
        _stack = stack;
    }
    // assignment
    public void RequireAssignment(ref ExprNode left, ref ExprNode right, OperatorToken opToken) {
        if (!CheckAssignment(ref left, ref right, opToken)) {
            var token = ExprNode.GetClosestToken(right);
            throw new IncompatibleTypesException(left.Type, right.Type, token.Lexeme, token.Line, token.Column);
        }
    }

    private bool CheckAssignment(ref ExprNode left, ref ExprNode right, OperatorToken opToken) {
        var op = opToken.Value;
        
        if (op == Constants.Operators.DivideAssign) {
            RequireCast(_stack.SymFloat, ref right);
        }

        switch (op) {
            case Constants.Operators.PlusAssign:
                CheckBinary(ref left, ref right,
                    new OperatorToken("+", Constants.Operators.Plus, opToken.Line, opToken.Column));
                break;
            
            case Constants.Operators.MinusAssign:
                CheckBinary(ref left, ref right,
                    new OperatorToken("-", Constants.Operators.Minus, opToken.Line, opToken.Column));
                break;
            
            case Constants.Operators.MultiplyAssign:
                CheckBinary(ref left, ref right,
                    new OperatorToken("*", Constants.Operators.Multiply, opToken.Line, opToken.Column));
                break;
            
            case Constants.Operators.DivideAssign:
                CheckBinary(ref left, ref right,
                    new OperatorToken("/", Constants.Operators.Divide, opToken.Line, opToken.Column));
                break;
            
            case Constants.Operators.Assign:
                break;
            
            default:
                
                throw new ArgumentOutOfRangeException($"Unknown {opToken.Lexeme} operator in CheckAssingment");
        }
        return TryCast(left.Type, ref right);
    }
    
    // binary operations 
    public void RequireBinary(ref ExprNode left, ref ExprNode right, Token op) {
        if (!(CheckBinary(ref left, ref right, op) || CheckBinary(ref right, ref left, op))) {
            throw new OperatorNotOverloaded(left.Type, right.Type, op.StringValue, op.Line, op.Column);
        }
    }

    // tries to cast source to target's type
    private bool CheckBinary(ref ExprNode source, ref ExprNode target, Token op) {
        switch (op) {
            case OperatorToken operatorToken:
                
                switch (operatorToken.Value) { 
                    case Constants.Operators.Plus:
                    case Constants.Operators.Minus:
                    case Constants.Operators.Multiply:
                        return TryCast(source.Type, ref target);
                    
                    case Constants.Operators.Divide:
                        TryCast(_stack.SymFloat, ref target);
                        return true;
                }
                break;
//            case ReservedToken reservedToken:
//                break;
        }

        return false;
    }

    private void RequireCast(SymType targetType, ref ExprNode source) {
        if (!TryCast(targetType, ref source)) {
            var n = ExprNode.GetClosestToken(source);
            throw new IncompatibleTypesException(targetType, source.Type, n.Lexeme, n.Line, n.Column);
        }
    }

    // try cast target to source
    private bool TryCast(SymType targetType, ref ExprNode source) {
        switch (targetType) {
            // scalars
            // float
            case SymFloat _:
                switch (source.Type) {
                    case SymInt _:
                        var t = ExprNode.GetClosestToken(source);
                        source = new CastNode(new IdentifierToken(_stack.SymFloat.Name, t.Line, t.Column), source);
                        source.Type = _stack.SymFloat;
                        return true;

                    case SymFloat _:
                        return true;
                }

                break;
            // end float
            // int
            case SymInt _:
                switch (source.Type) {
                    case SymInt _:
                        return true;
                }

                break;
            // end int
            // char
            
            case SymChar _ :
                switch (source.Type) {
                    case SymChar _:
                        return true;
                }
                
                break;
            // end char
        }

        return false;
    }

}
}