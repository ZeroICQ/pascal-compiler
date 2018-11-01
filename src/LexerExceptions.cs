using System;

namespace Compiler {

public abstract class LexerException : Exception {};

public class UnkownLexemeException: LexerException {
    public int Line { get; protected set;  }
    public int Column { get; protected set;  }

    public UnkownLexemeException(int line, int column) {
        Line = line;
        Column = column;
    }
}

}