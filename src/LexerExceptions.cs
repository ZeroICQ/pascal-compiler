using System;
using Compiler;

namespace Compiler {

public abstract class LexerException : Exception {
    public int Line { get; }
    public int Column { get; }
    public string Lexeme { get; }
    public abstract override string Message { get; }

    protected LexerException(string lexeme, int line, int column) {
        Line = line;
        Column = column;
        Lexeme = lexeme;
    }
};

public class UnknownLexemeException : LexerException {
    public override string Message => $"Unknown lexeme \"{Lexeme}\" at {Line.ToString()},{Column.ToString()}";
    
    public UnknownLexemeException(string lexeme, int line, int column) : base(lexeme, line, column) {}
}

public class UnclosedCommentException : LexerException {
    public override string Message => $"Unclosed comment \"{Lexeme}\" at {Line.ToString()},{Column.ToString()}";

    public UnclosedCommentException(string lexeme, int line, int column) : base(lexeme, line, column) {}
}

}

public class StringExceedsLineException : LexerException {
    public override string Message => $"String exceeds line at {Line.ToString()},{Column.ToString()}";
    
    public StringExceedsLineException(string lexeme, int line, int column) : base(lexeme, line, column) {}
}

public class StringMalformedException : LexerException {
    public override string Message => $"Malformed string at {Line.ToString()},{Column.ToString()}";

    public StringMalformedException(string lexeme, int line, int column) : base(lexeme, line, column) {}
}

public class IntegerLiteralOverflowException : LexerException {
    public override string Message => $"Range check error at {Line},{Column}. Must be between {long.MinValue} and {long.MaxValue}";

    public IntegerLiteralOverflowException(string lexeme, int line, int column) : base(lexeme, line, column) {}
}

public class RealLiteralOverflowException : LexerException {
    public override string Message => $"Range check error at {Line},{Column}. Must be between {double.MinValue} and {double.MaxValue}";

    public RealLiteralOverflowException(string lexeme, int line, int column) : base(lexeme, line, column) {}
}
