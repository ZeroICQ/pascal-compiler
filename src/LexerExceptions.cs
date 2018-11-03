using System;

namespace Compiler {

public abstract class LexerException : Exception {
    public int Line { get; }
    public int Column { get; }
    public string Lexeme { get; }

    protected LexerException(string lexeme, int line, int column) {
        Line = line;
        Column = column;
        Lexeme = lexeme;
    }
};

public class UnknownLexemeException: LexerException {
    public UnknownLexemeException(string lexeme, int line, int column) : base(lexeme, line, column) {}
}

public class CommentNotClosedException : LexerException {
    public CommentNotClosedException(string lexeme, int line, int column) : base(lexeme, line, column) {}
}

}