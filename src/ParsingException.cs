using System;

namespace Compiler {

public abstract class ParsingException : Exception {
    public int Line { get; }
    public int Column { get; }
    public string Lexeme { get; }
    public abstract override string Message { get; }

    protected ParsingException(string lexeme, int line, int column) {
        Line = line;
        Column = column;
        Lexeme = lexeme;
    }
};

}