using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Compiler {

internal static class Symbols {
    // ASK: why can't var?
    public static readonly HashSet<char> digits = new HashSet<char>("0123456789".ToCharArray());
    public static readonly HashSet<char> letters = new HashSet<char>("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz".ToCharArray());
    public static readonly HashSet<char> hexDigits = new HashSet<char>("ABCDEFabcdef0123456789".ToCharArray());
    public static readonly HashSet<char> separators = new HashSet<char> {'\t', ' ', '\n', '\r'};
    public static int EOF = -1;
}


public class LexemesAutomata {
    private enum States {Start, Id, Digit, Separator, Eof, Comment}
    
    private int _line = 1;
    private int _column = 1;
            
    public Token Parse(StreamReader input) {
        var currState = States.Start;
        
        while (true) {
            var eof = input.Peek() == -1;
            var forward = (char) input.Peek();
            
            switch (currState) {
                case States.Start:
                    if (eof)
                        currState = States.Eof;
                    else if (Symbols.letters.Contains(forward) || forward == '_')
                        currState = States.Id;
                    else if (Symbols.digits.Contains(forward))
                        currState = States.Digit;
                    else if (Symbols.separators.Contains(forward))
                        currState = States.Separator;
                    else if (forward == '/' || forward == '{')
                        currState = States.Comment;
                    break;
                case States.Id:
                    return IdAutomata.Parse(input, ref _line, ref _column);
                case States.Digit:
                    break;
                case States.Comment:
                    CommentAutomata.Parse(input, ref _line, ref _column);
                    currState = States.Start;
                    break;
                case States.Separator:
                    if (eof) {
                        currState = States.Eof;
                        break;
                    } else if (!Symbols.separators.Contains(forward)) {
                        currState = States.Start;
                        break;
                    } else if (forward == '\n') {
                        _line += 1;
                        _column = 1;
                    } else
                        _column += 1;
                        
                    input.Read();
                    
                    break;
                case States.Eof:
                    return null;
                default:
                    throw new ArgumentOutOfRangeException();
            }                
        }
    }
}


public static class IdAutomata {
    private enum States {Start, Body, Finish}
    
    public static Token Parse(StreamReader input, ref int line, ref int column) {
        var currState = States.Start;
        var value = new StringBuilder();
        
        while (true) {
            var eof = input.Peek() == -1;
            var forward = (char) input.Peek();
            
            switch (currState) {
                case States.Start:
                    value.Append(forward);
                    input.Read();
                    currState = States.Body;
                    break;
                case States.Body:
                    if (Symbols.letters.Contains(forward) || Symbols.digits.Contains(forward) || forward == '_') {
                        value.Append(forward);
                        input.Read();
                    } else
                        currState = States.Finish;
                    break;
                case States.Finish:
                    var identityToken = new IdentityToken(value.ToString(), line, column);
                    column += value.Length;
                    return identityToken;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}

// TODO: comments starting (*
public static class CommentAutomata {
    private enum States {Start, Curly, Line, Finish}
    
    public static void Parse(StreamReader input, ref int line, ref int column) {
        var currState = States.Start;
        
        while (true) {
            var eof = input.Peek() == -1;
            var forward = (char) input.Peek();

            switch (currState) {
                case States.Start:
                    if (forward == '/')
                        currState = States.Line;
                    else if (forward == '{')
                        currState = States.Curly;
                    break;
                case States.Curly:
                    if (eof) {
                        return;
                    }
                    
                    if (forward == '}') {
                        input.Read();
                        column += 1;
                        return;
                    }

                    if (forward == '\n') {
                        line += 1;
                        column = 1;
                    }
                    else if (forward != '\r')
                        column += 1;

                    input.Read();
                    
                    break;
                case States.Line:
                    if (eof) {
                        return;
                    }
                    
                    if (forward == '\n') {
                        input.Read();
                        line += 1;
                        return;
                    }

                    input.Read();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}

} // namespace Compiler  
