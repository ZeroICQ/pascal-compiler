using System.Collections;

namespace Compiler {
public class TypeChecker {
    private readonly SymStack _stack;

    public TypeChecker(SymStack stack) {
        _stack = stack;
    }
    // assignment
    public void RequireAssignment(ref ExprNode left, ref ExprNode right, Constants.Operators op) {
        if (!CheckAssignment(ref left, ref right, op)) {
            var token = ExprNode.GetClosestToken(right);
            throw new IncompatibleTypesException(left.Type, right.Type, token.Lexeme, token.Line, token.Column);
        }
    }

    private bool CheckAssignment(ref ExprNode left, ref ExprNode right, Constants.Operators op) {
        // todo: assign can also be +=, -= etc...
        return TryCast(ref left, ref right);
    }
    
    // binary operations 
    public void RequireBinary(ref ExprNode left, ref ExprNode right, Token op) {
        if (!CheckBinary(ref left, ref right, op)) {
            throw new OperatorNotOverloaded(left.Type, right.Type, op.StringValue, op.Line, op.Column);
        }
    }

    private bool CheckBinary(ref ExprNode left, ref ExprNode right, Token op) {
        switch (op) {
            case OperatorToken operatorToken:
                
                switch (operatorToken.Value) { 
                    case Constants.Operators.Plus:
                    case Constants.Operators.Minus:
                    case Constants.Operators.Divide:
                    case Constants.Operators.Multiply:
                        var leftToRightTry = TryCast(ref left, ref right);
                        
                        if (!leftToRightTry) {
                            return TryCast(ref right, ref left);
                        }

                        return leftToRightTry;
                }
                break;
//            case ReservedToken reservedToken:
//                break;
        }

        return false;
    }

    // try cast target to source
    private bool TryCast(ref ExprNode source, ref ExprNode target) {
        switch (source.Type) {
            // scalars
            // float
            case SymFloat _:
                switch (target.Type) {
                    case SymInt _:
                        target = new CastNode(new IdentifierToken(_stack.SymFloat.Name, 0, 0), target);
                        target.Type = _stack.SymFloat;
                        return true;

                    case SymFloat _:
                        return true;
                }

                break;
            // end float
            // int
            case SymInt _:
                switch (target.Type) {
                    case SymInt _:
                        return true;
                }

                break;
            // end int
        }

        return false;
    }

}
}