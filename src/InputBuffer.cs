using System;
using System.IO;

namespace Compiler {

public class InputBuffer {
    public int Column { get; protected set; } = 1;
    public int Line { get; protected set; } = 1;
    
    private readonly TextReader _textReader;

    public int Read() {
        SkipWhitespaces();
        return _textReader.Read();
    }

    public int Peek() {
        return _textReader.Peek();
    }
    
    public InputBuffer(TextReader textReader) {
        _textReader = textReader;
    }

    public void SkipLine() {
        while (true) {
            var symbol = _textReader.Read();

            switch (symbol) {
                case '\n':
                    Line += 1;
                    Column = 1;
                    return;
                case '\r':
                    break;
                case -1:
                    return;
                default:
                    Column += 1;
                    break;
            }
        }
            
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
 