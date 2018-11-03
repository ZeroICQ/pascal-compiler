using System.IO;
using System.Text;

namespace Compiler {

public class InputBuffer {
    public int Column { get; protected set; } = 1;
    public int Line { get; protected set; } = 1;

    public int LexemeBeginLine { get; protected set; } = 1;
    public int LexemeBeginColumn { get; protected set; } = 1;
    
    public string Lexeme => _buffer.ToString();

    private readonly TextReader _textReader;
    private readonly StringBuilder _buffer;
    
    // retraction simulation
    private bool _isRetracted = false;
    private int lastSymbol = -1;
    private int _prevLineColumn = 1;

    public void StartLexeme() {
        SkipWhitespaces();
        
        LexemeBeginLine = Line;
        LexemeBeginColumn = Column;

        _buffer.Clear();
    }

    public void Retract() {
        _isRetracted = true;

        switch (lastSymbol) {
            case '\n':
                Column = _prevLineColumn;
                Line = Line > 1 ? Line - 1 : Line;
                break;
            case '\r':
            case -1:
                //do nothing
                break;
            default:
                Column = Column > 1 ? Column - 1 : Column;
                break;
        }

        //remove last symbol from lexeme buffer
        if (_buffer.Length > 0) {
            _buffer.Remove(_buffer.Length - 1, 1);
        }
    }

    public int Read() {
        var symbol = ReadNext();
        lastSymbol = symbol;
        
        if (symbol != -1)
            _buffer.Append((char) symbol);
        
        switch (symbol) {
            case '\n':
                _prevLineColumn = Column;
                
                Line += 1;
                Column = 1;
                break;
            case '\r':
                break;
            // EOF
            case -1:
                break;
            default:
                Column += 1;
                break;
        }
        
        return symbol;
    }

    public int Peek() {
        return _isRetracted ? lastSymbol : _textReader.Peek();
    }

    public void SkipLine() {
        while (true) {
            var symbol = ReadNext();

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

    public InputBuffer(TextReader textReader) {
        _textReader = textReader;
    }

    private void SkipWhitespaces() {
        while (true) {
            var peek = Peek();

            switch (peek) {
                case '\t':
                case ' ':
                    Column += 1;
                    _textReader.Read();
                    break;
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

    private int ReadNext() {
        if (!_isRetracted) return _textReader.Read();
        
        _isRetracted = false;
        return lastSymbol;
    }
}

} //namespace Compiler
 