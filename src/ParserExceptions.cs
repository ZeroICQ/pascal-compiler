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

public class IncompatibleTypesException : ParserException {
    private SymType _leftType;
    private SymType _rightType;
    public override string Message => $"Types \"{_leftType.Name}\" and \"{_rightType.Name}\" are incompatible at {Line},{Column}.";

    public IncompatibleTypesException(SymType leftType, SymType rightType, string lexeme, int line, int column)
        : base(lexeme, line, column) {
        _leftType = leftType;
        _rightType = rightType;
    }
}

public class NotLvalueException : ParserException {
    public override string Message => $"Expression is not lvalue at {Line},{Column}";
    public NotLvalueException(string lexeme, int line, int column) : base(lexeme, line, column) { }
}

public class ParserPanicException : Exception {
    public override string Message => "This error must not be thrown under any circumstances";
}

}