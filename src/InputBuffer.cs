using System;
using System.IO;

namespace Compiler {

public class InputBuffer {
    public int Column { get; protected set; } = 1;
    public int Line { get; protected set; } = 1;
    
    private readonly TextReader _textReader;

    public int Read() {
        SkipWhitespaces();
        var c = _textReader.Read();
        
        if (c == '\n') {
            Line += 1;
            Column = 1;
        } else if (c != '\r' && c != -1) {
            Column += 1;
        }

        return c;
    }

    public int Peek() {
        return _textReader.Peek();
    }
    
    public InputBuffer(TextReader textReader) {
        _textReader = textReader;
    }

    private void SkipWhitespaces() {
        while (true) {
            var peek = _textReader.Peek();

            switch (peek) {
                case ' ':
                    Column += 1;
                    _textReader.Read();
                    break;
                case '\t':
                    goto case ' ';
                case '\r':
                    _textReader.Read();
                    break;
                case '\n':
                    Column = 1;
                    Line += 1;
                    break;
                default:
                    return;
            }
        }
    }
}

} //namespace Compiler
 