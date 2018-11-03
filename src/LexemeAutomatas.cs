using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Compiler {

internal static class Symbols {
    public static readonly HashSet<char> decDigits = new HashSet<char>("0123456789".ToCharArray());
    public static readonly HashSet<char> octDigits = new HashSet<char>("01234567".ToCharArray());
    public static readonly HashSet<char> hexDigits = new HashSet<char>("01234567ABCDEFabcdef".ToCharArray());
    public static readonly HashSet<char> letters = new HashSet<char>("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz".ToCharArray());
// TODO: separators are not whitespaces
//    public static readonly HashSet<char> separators = new HashSet<char> {'\t', ' ', '\n', '\r'};
    public const int EOF = -1;
}

//{} - braces, [] - brackets, () - parentheses

public class LexemesAutomata {
    private readonly InputBuffer _input;

    public LexemesAutomata(InputBuffer input) {
        _input = input;
    }
    
    private enum States {Start, AfterSlash, AfterParenthesis}
    public Token Parse() {
        var currState = States.Start;
        //ASK: does pascal skip whitespace between lexemes?
        
        while (true) {
            //must be performed before Read(), due to whitespace skipping in startLexeme()
            if (currState == States.Start)
                _input.StartLexeme();
            
            var symbol = _input.Read();
            
            switch (currState) {
                case States.Start:
                    
                    if (symbol == Symbols.EOF)
                        return new EofToken();
                    
                    else if (symbol == '/')
                        currState = States.AfterSlash;
                    
                    else if (symbol == '{')
                        BracesCommentAutomata.Parse(_input);

                    else if (symbol == '(')
                        currState = States.AfterParenthesis;
                    
                    else if (Symbols.letters.Contains((char) symbol) || symbol == '_')
                        return IdentityAutomata.Parse(_input);
                    
                    else
                        throw new UnknownLexemeException(_input.Lexeme, _input.LexemeLine, _input.LexemeColumn);
                    
                    break;
                    // --- END OF States.Start ---
                
                case States.AfterSlash:

                    if (symbol == '/') {
                        _input.SkipLine();
                        currState = States.Start;
                    } 
                    else
                        throw new UnknownLexemeException(_input.Lexeme, _input.LexemeLine, _input.LexemeColumn);
                    
                    break;
                    // --- END OF States.AfterSlash ---
                
                case States.AfterParenthesis:
                    if (symbol == '*') {
                        ParenthesesComments.Parse(_input);
                        currState = States.Start;
                    }
                    else
                        throw new UnknownLexemeException(_input.Lexeme, _input.LexemeLine, _input.LexemeColumn);
                    
                    break;
                    // -- END OF States.AfterParenthesis
                default:
                    throw new ArgumentOutOfRangeException();
            }                
        }
    }
}

// Start position is [a-zA-z_]->[...]  
public static class IdentityAutomata {
    public static Token Parse(InputBuffer input) {
        while (true) {
            var symbol = input.Read();

            if (Symbols.letters.Contains((char) symbol) || Symbols.decDigits.Contains((char) symbol) || symbol == '_') 
                continue;
            
            if (symbol != Symbols.EOF)
                input.Retract();
            //todo: check for keyword
            return new IdentityToken(input.Lexeme, input.LexemeLine, input.LexemeColumn);
        }
    }
}

// Start position after brace {->[...]
public static class BracesCommentAutomata {
    
    public static void Parse(InputBuffer input) {
        while (true) {
            var symbol = input.Read(isWriteToBuffer: false);
            
            switch (symbol) {
                case Symbols.EOF:
                    throw new CommentNotClosedException(input.Lexeme, input.LexemeLine, input.LexemeColumn);
                case '}':
                    return;
            }
        }
    }
}

// Start position is after asterisk (*->[...] 
public static class ParenthesesComments {
    private enum States {InsideComment, AfterAsterisk}

    public static void Parse(InputBuffer input) {
        var currState = States.InsideComment;
        
        while (true) {
            var symbol = input.Read(false);
            
            switch (currState) {
                case States.InsideComment:
                    if (symbol == Symbols.EOF) {
                        throw new CommentNotClosedException(input.Lexeme, input.LexemeLine, input.LexemeColumn);
                    }
                    else if (symbol == '*')
                        currState = States.AfterAsterisk;
                    break;
                
                case States.AfterAsterisk:
                    if (symbol == Symbols.EOF) {
                        throw new CommentNotClosedException(input.Lexeme, input.LexemeLine, input.LexemeColumn);
                    }
                    else if (symbol == ')') {
                        return;
                    }
                    else
                        currState = States.InsideComment;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
} //namespace Compiler  
