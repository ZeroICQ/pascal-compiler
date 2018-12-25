using System;

namespace Compiler {

public abstract class ParserException : ParsingException {
    protected ParserException(string lexeme, int line, int column) : base(lexeme, line, column) {}
};

public class IllegalExprException : ParserException {
    public override string Message { get; } 

    public IllegalExprException(string lexeme, int line, int column) : base(lexeme, line, column) {
        Message = $"Illegal expression {Lexeme} at {Line},{Column}.";
    }
    
    public IllegalExprException(string lexeme, int line, int column, string expected) : base(lexeme, line, column) {
        Message = $"Illegal expression {Lexeme} at {Line},{Column}. Expected {expected}.";
    }
    
}

public class NotAllowedException : ParserException {
    public override string Message => $"{Lexeme} not allowed at {Line},{Column}.";
    public NotAllowedException(string lexeme, int line, int column) : base(lexeme, line, column) { }
}

public class TypeNotFoundException : ParserException {
    public override string Message => $"Type {Lexeme} at {Line},{Column} was not found."; 
    public TypeNotFoundException(string lexeme, int line, int column) : base(lexeme, line, column) { }
}

public class DuplicateIdentifierException : ParserException {
    public override string Message => $"Duplicate identifier {Lexeme} at {Line},{Column}.";
    public DuplicateIdentifierException(string lexeme, int line, int column) : base(lexeme, line, column) { }
}

public class IdentifierNotDefinedException : ParserException {
    public override string Message => $"Identifier {Lexeme} at {Line},{Column} was not found in this scope.";

    public IdentifierNotDefinedException(string lexeme, int line, int column) : base(lexeme, line, column) {
        
    }
}

public class MemberNotFoundException : ParserException {
    private readonly SymRecord _record;
    private readonly IdentifierToken _field;

    public override string Message =>
        $"Record type \"{_record.Name}\" doesn't have member \"{_field.Value}\" at {Line},{Column}.";
    public MemberNotFoundException(SymRecord record, IdentifierToken field, string lexeme, int line, int column) 
        : base(lexeme, line, column) 
    {
        _record = record;
        _field = field;
    }
}

public class IncompatibleTypesException : ParserException {
    private readonly SymType _leftType;
    private readonly SymType _rightType;
    public override string Message => $"Types \"{_leftType.Name}\" and \"{_rightType.Name}\" are incompatible at {Line},{Column}.";

    public IncompatibleTypesException(SymType leftType, SymType rightType, string lexeme, int line, int column)
        : base(lexeme, line, column) {
        _leftType = leftType;
        _rightType = rightType;
    }
}

public class OperatorNotOverloaded : ParserException {
    public override string Message { get; }
    
    public OperatorNotOverloaded(SymType leftType, SymType rightType, string lexeme, int line, int column) : base(lexeme, line, column) {
        Message =
            $"Operator {Lexeme} is not overloaded for \"{leftType.Name}\" and \"{rightType.Name}\" at {Line},{Column}.";
    }
    
    public OperatorNotOverloaded(SymType type, string lexeme, int line, int column) : base(lexeme, line, column) {
        Message = $"Operator {Lexeme} is not overloaded for \"{type.Name}\" at {Line},{Column}.";
    }
}

public class NotLvalueException : ParserException {
    public override string Message => $"Expression is not lvalue at {Line},{Column}.";
    public NotLvalueException(string lexeme, int line, int column) : base(lexeme, line, column) { }
}

public abstract class ConstExprEvalException : ParserException {
    protected ConstExprEvalException(string lexeme, int line, int column) : base(lexeme, line, column) { }
}

public class NamedConstExprEvalException : ConstExprEvalException {
    public override string Message => $"Could not evaluate const expression for {Lexeme} at {Line},{Column}.";
    public NamedConstExprEvalException(string lexeme, int line, int column) : base(lexeme, line, column) { }
}

public class AnonConstExprEvalException : ConstExprEvalException {
    public override string Message => $"Could not evaluate const expression at {Line},{Column}.";
    public AnonConstExprEvalException(string lexeme, int line, int column) : base(lexeme, line, column) { }
}

public class UpperRangeBoundLessThanLowerException : ParserException {
    public override string Message => $"Upper bound of range is less than lower bound at {Line},{Column}.";
    public UpperRangeBoundLessThanLowerException(string lexeme, int line, int column) : base(lexeme, line, column) { }
}

public class ArrayExpectedException : ParserException {
    private readonly SymType _type;
    public override string Message => $"Array expected. Got {_type.Name} at {Line}, {Column}.";
    public ArrayExpectedException(SymType type, string lexeme, int line, int column) : base(lexeme, line, column) {
        _type = type;
    }
}

public class RecordExpectedException : ParserException {
    private SymType _gotType;
    public override string Message => $"Record expected. Got {_gotType.Name} at {Line}, {Column}.";

    public RecordExpectedException(SymType gotType, string lexeme, int line, int column) : base(lexeme, line, column) {
        _gotType = gotType;
    }
}

public class RangeCheckErrorException : ParserException {
    private readonly long _gotIndex;
    private readonly long _minIndex;
    private readonly long _maxIndex;
    
    public override string Message => $"Range check error {_gotIndex} must be between {_minIndex} and {_maxIndex}";
    
    public RangeCheckErrorException(long gotIndex, long minIndex, long maxIndex, int line, int column) 
        : base(gotIndex.ToString(), line, column) {
        _gotIndex = gotIndex;
        _minIndex = minIndex;
        _maxIndex = maxIndex;
    }
}

public class ParserPanicException : Exception {

    public override string Message => "This error must not be thrown under any circumstances.";
}


}