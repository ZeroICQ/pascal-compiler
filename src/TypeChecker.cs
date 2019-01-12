using System;
using System.Collections.Generic;

namespace Compiler {
public class TypeChecker {
    private readonly SymStack _stack;

    public TypeChecker(SymStack stack) {
        _stack = stack;
    }

    public SymFunc RequireFunction(Token token, SymFunc funcSym, List<ExprNode> args) {
        var argc = args.Count;
        var parc = funcSym.Parameters.Count;
        
        if (argc != parc)
            throw new WrongArgumentsNumberException(parc, argc, token.Lexeme, token.Line, token.Column);
        
        for (var i = 0; i < argc; i++) {
            var currParameter = funcSym.Parameters[i];
            var curArgument  = args[i];
            
            switch (funcSym.Parameters[i].VarType) {
                case SymVar.VarTypeEnum.Global:
                case SymVar.VarTypeEnum.Local:
                    throw new WrongParameterTypeException();
                
                case SymVar.VarTypeEnum.Parameter:
                    RequireCast(currParameter.Type, ref curArgument);
                    args[i] = curArgument;
                    break;
                case SymVar.VarTypeEnum.VarParameter:
                case SymVar.VarTypeEnum.OutParameter:
                case SymVar.VarTypeEnum.ConstParameter:
                    throw new NotImplementedException();
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
        }

        return funcSym;
    }

    
    public SymType RequireAccess(ExprNode recRef, IdentifierToken fieldName) {
        var exprToken = ExprNode.GetClosestToken(recRef);
        // aliases
        var realType = recRef.Type;

        if (realType is SymTypeAlias symTypeAlias)
            realType = symTypeAlias.Type;
        
        if (!(realType is SymRecord record)) {
            throw new RecordExpectedException(realType, exprToken.Lexeme, exprToken.Line, exprToken.Column);
        }

        var fieldSymbol = record.Fields.Find(fieldName.Value);
        
        if (fieldSymbol == null || !(fieldSymbol is SymVar symVar))
            throw new MemberNotFoundException(record, fieldName, fieldName.Lexeme, fieldName.Line, fieldName.Column);
        
        return symVar.Type;
    }
    
    public void RequireAssignment(ref ExprNode left, ref ExprNode right, OperatorToken opToken) {
        if (CheckAssignment(ref left, ref right, opToken)) 
            return;
        var token = ExprNode.GetClosestToken(right);
        throw new IncompatibleTypesException(left.Type, right.Type, token.Lexeme, token.Line, token.Column);
    }

    private bool CheckAssignment(ref ExprNode left, ref ExprNode right, OperatorToken opToken) {
        var op = opToken.Value;
        
        switch (op) {
            case Constants.Operators.PlusAssign:
                RequireBinary(ref left, ref right,
                    new OperatorToken("+", Constants.Operators.Plus, opToken.Line, opToken.Column));
                break;
            
            case Constants.Operators.MinusAssign:
                RequireBinary(ref left, ref right,
                    new OperatorToken("-", Constants.Operators.Minus, opToken.Line, opToken.Column));
                break;
            
            case Constants.Operators.MultiplyAssign:
                RequireBinary(ref left, ref right,
                    new OperatorToken("*", Constants.Operators.Multiply, opToken.Line, opToken.Column));
                break;
            
            case Constants.Operators.DivideAssign:
                RequireBinary(ref left, ref right,
                    new OperatorToken("/", Constants.Operators.Divide, opToken.Line, opToken.Column));
                break;
            
            case Constants.Operators.Assign:
                break;
            
            default:
                throw new ArgumentOutOfRangeException($"Unknown {opToken.Lexeme} operator in CheckAssignment");
        }
        return TryCast(left.Type, ref right);
    }
    
    // binary operations 
    public void RequireBinaryAny(ref ExprNode left, ref ExprNode right, Token op) {
        // division special case int / int;
        if (op is OperatorToken opToken && opToken.Value == Constants.Operators.Divide) {
            if (!(CheckBinary(ref left, ref right, op) && CheckBinary(ref right, ref left, op))) {
                throw new OperatorNotOverloaded(left.Type, right.Type, op.Lexeme, op.Line, op.Column);
            }
        }
        
        if (!(CheckBinary(ref left, ref right, op) || CheckBinary(ref right, ref left, op))) {
            throw new OperatorNotOverloaded(left.Type, right.Type, op.Lexeme, op.Line, op.Column);
        }
    }
   
    
    public void RequireBinary(ref ExprNode left, ref ExprNode right, Token op) {
        if (!CheckBinary(ref left, ref right, op)) {
            throw new OperatorNotOverloaded(left.Type, right.Type, op.Lexeme, op.Line, op.Column);
        }
    }

    // tries to cast target to source's type
    private bool CheckBinary(ref ExprNode source, ref ExprNode target, Token op) {
        //todo: xor, shl, shr, etc...
        var realSourceType = source.Type is SymTypeAlias sourceTypeAlias ? sourceTypeAlias.Type : source.Type;
//        var realTargetType = target.Type is SymTypeAlias targetTypeAlias ? targetTypeAlias.Type : target.Type;
        
        switch (op) {
            case OperatorToken operatorToken:
                switch (operatorToken.Value) {
                    
                    case Constants.Operators.Plus:
                    case Constants.Operators.Minus:
                    case Constants.Operators.Multiply:
                        return realSourceType is SymScalar && !(realSourceType is SymChar || realSourceType is SymBool)
                                                        && TryCast(realSourceType, ref target);
                    
                    case Constants.Operators.Divide:
                        return realSourceType is SymScalar && !(realSourceType is SymChar || realSourceType is SymBool) && 
                               TryCast(_stack.SymDouble, ref source, false) && TryCast(_stack.SymDouble, ref target);
                    
                    case Constants.Operators.Less:
                    case Constants.Operators.LessOrEqual:
                    case Constants.Operators.More:
                    case Constants.Operators.MoreOreEqual:
                    case Constants.Operators.Equal:
                    case Constants.Operators.NotEqual:
                        return realSourceType is SymScalar && !(realSourceType is SymChar) && TryCast(realSourceType, ref target);
                    
                }
                break;
            
            case ReservedToken reservedToken:
                switch (reservedToken.Value) {
                    
                    case Constants.Words.Div:
                    case Constants.Words.Mod:
                        RequireCast(_stack.SymInt, ref target);
                        RequireCast(_stack.SymInt, ref source);
                        return true;
                    
                    case Constants.Words.And:
                    case Constants.Words.Or:
                        // only bool and int
                        return (realSourceType is SymBool || realSourceType is SymInt) && TryCast(realSourceType, ref target);
                }
                break;
        }
        return false;
    }
    
    public void RequireCast(SymType targetType, ref ExprNode source) {
        if (TryCast(targetType, ref source)) 
            return;
        
        var token = ExprNode.GetClosestToken(source);
        throw new IncompatibleTypesException(targetType, source.Type, token.Lexeme, token.Line, token.Column);
    }

    public bool CanCast(SymType targetType, ExprNode source) {
        return TryCast(targetType, ref source, false);
    }

    // try cast target to source
    public bool TryCast(SymType targetType, ref ExprNode source, bool canModify = true) {
        var realSourceType = source.Type is SymTypeAlias sourceTypeAlias ? sourceTypeAlias.Type : source.Type;
        var realTargetType = targetType is SymTypeAlias targetTypeAlias ? targetTypeAlias.Type : targetType;
        
        switch (realTargetType) {
            // scalars
            // double
            case SymDouble _:
                switch (realSourceType) {
                    case SymInt _:
                        var t = ExprNode.GetClosestToken(source);
                        if (!canModify) 
                            return true;
                        
                        source = new CastNode(_stack.SymDouble, source);
                        source.Type = _stack.SymDouble;
                        return true;

                    case SymDouble _:
                        return true;
                }

                break;
            // end double
            // int
            case SymInt _:
                switch (realSourceType) {
                    case SymInt _:
                        return true;
                }

                break;
            // end int
            // char
            case SymChar _ :
                switch (realSourceType) {
                    case SymChar _:
                        return true;
                }
                
                break;
            // end char
            // bool
            case SymBool _ :
                switch (realSourceType) {
                    case SymBool _ :
                        return true;
                }
                
                break;
            // end bool
            case SymArray target:
                switch (realSourceType) {
                    case SymArray sourceType:
                        return IsTypesEqual(target, sourceType); 
                }
                break;
            default:
                return IsTypesEqual(realSourceType, realTargetType);
        }
        
        return false;
    }

    private bool IsTypesEqual(SymType target, SymType source) {
        var lhs = target;
        var rhs = source;

        while (true) {
            switch (lhs) {
                case SymArray lArr:

                    switch (rhs) {
                        case SymArray rArr:
                            if (lArr.MinIndex.Value != rArr.MinIndex.Value ||
                                lArr.MaxIndex.Value != rArr.MaxIndex.Value) {
                                return false;
                            }
                            
                            if (lArr.Type is SymScalar && rArr.Type is SymScalar) {
                                return lArr.Type.GetType()   == rArr.Type.GetType();
                            }

                            lhs = lArr.Type;
                            rhs = rArr.Type;
                            
                        continue;
                    }
                    
                    break;
                
                case SymScalar lScalar:
                    switch (rhs) {
                        case SymScalar rScalar:
                            return lScalar.GetType() == rScalar.GetType();
                    }
                    break;
                
                case SymRecord lRecord:
                    switch (rhs) {
                        case SymRecord rRecord:
                            return lRecord.Name == rRecord.Name;
                    }
                    break;
                default:
                    return lhs.GetType() == rhs.GetType();
            }

            return false;
        }
    }
}
}