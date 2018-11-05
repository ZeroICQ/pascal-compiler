using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq.Expressions;
using System.Text;

namespace Compiler {

internal static class Symbols {
    public static readonly HashSet<char> decDigits = new HashSet<char>("0123456789".ToCharArray());
    public static readonly HashSet<char> octDigits = new HashSet<char>("01234567".ToCharArray());
    public static readonly HashSet<char> hexDigits = new HashSet<char>("0123456789ABCDEFabcdef".ToCharArray());
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

    private enum States {
        Start, AfterSlash, AfterParenthesis, AfterAmpersand, AfterLess, AfterMore, AfterStar, AfterColon,
        AfterPlus, AfterMinus, AfterDot
    }
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
                        return new EofToken(_input.LexemeLine, _input.LexemeColumn);
                    
                    else if (symbol == '/')
                        currState = States.AfterSlash;
                    
                    else if (symbol == '{')
                        BracesCommentAutomata.Parse(_input);

                    else if (symbol == '(')
                        currState = States.AfterParenthesis;
                    
                    else if (symbol == '$')
                        return HexNumberAutomata.Parse(_input);
                    
                    else if (symbol == '&') {
                        currState = States.AfterAmpersand;
                    }
                    
                    else if (symbol == '%') {
                        return BinaryNumberAutomata.Parse(_input);
                    }
                    
                    else if (symbol == '\'') {
                        return StringAutomata.Parse(_input, StringAutomata.States.QuotedString);
                    }
                    
                    else if (symbol == '#') {
                        return StringAutomata.Parse(_input, StringAutomata.States.AfterHash);
                    }
                    
                    else if (Symbols.letters.Contains((char) symbol) || symbol == '_')
                        return IdentityAutomata.Parse(_input);
                    
                    else if (Symbols.decDigits.Contains((char) symbol))
                        return DecimalNumberAutomata.Parse(_input);
                    
                    // --- OPERATORS AND SEPARATORS ---
                    else if (symbol == '+')
                        currState = States.AfterPlus;
                    
                    else if (symbol == '-')
                        currState = States.AfterMinus;
                    
                    else if (symbol == '*')
                        currState = States.AfterStar;
                    
                    else if (symbol == '=')
                        return new OperatorToken(_input.Lexeme, OperatorToken.Operation.Equal, _input.LexemeLine, _input.LexemeColumn);
                    
                    else if (symbol == '<')
                        currState = States.AfterLess;
                    
                    else if (symbol == '>')
                        currState = States.AfterMore;
                    
                    else if (symbol == '[')
                        return new OperatorToken(_input.Lexeme, OperatorToken.Operation.OpenBracket, _input.LexemeLine, _input.LexemeColumn);
                    
                    else if (symbol == ']')
                        return new OperatorToken(_input.Lexeme, OperatorToken.Operation.CloseBracket, _input.LexemeLine, _input.LexemeColumn);
                    
                    else if (symbol == '.')
                        currState = States.AfterDot;
                    
                    else if (symbol == ')')
                        return new OperatorToken(_input.Lexeme, OperatorToken.Operation.CloseParenthesis, _input.LexemeLine, _input.LexemeColumn);
                    
                    else if (symbol == '^')
                        return new OperatorToken(_input.Lexeme, OperatorToken.Operation.Caret, _input.LexemeLine, _input.LexemeColumn);
                    
                    else if (symbol == '@')
                        return new OperatorToken(_input.Lexeme, OperatorToken.Operation.AtSign, _input.LexemeLine, _input.LexemeColumn);
                    
                    else if (symbol == ';')
                        return new SeparatorToken(_input.Lexeme, SeparatorToken.Separator.Semicolon, _input.LexemeLine, _input.LexemeColumn);
                    
                    else if (symbol == ',')
                        return new SeparatorToken(_input.Lexeme, SeparatorToken.Separator.Comma, _input.LexemeLine, _input.LexemeColumn);
                    
                    else if (symbol == ':')
                        currState = States.AfterColon;
                    
                    
                    else
                        throw new UnknownLexemeException(_input.Lexeme, _input.LexemeLine, _input.LexemeColumn);
                    
                    break;
                    // --- END OF States.Start ---
                //start after  <
                case States.AfterLess:
                    if (symbol == '>')
                        return new OperatorToken(_input.Lexeme, OperatorToken.Operation.NotEqual, _input.LexemeLine, _input.LexemeColumn);
                    else if (symbol == '<')
                        return new OperatorToken(_input.Lexeme, OperatorToken.Operation.BitwiseShiftLeft, _input.LexemeLine, _input.LexemeColumn);
                    else if (symbol == '=')
                        return new OperatorToken(_input.Lexeme, OperatorToken.Operation.LessOrEqual, _input.LexemeLine, _input.LexemeColumn);
                    else {
                        _input.Retract();
                        return new OperatorToken(_input.Lexeme, OperatorToken.Operation.Less, _input.LexemeLine, _input.LexemeColumn);
                    }
                //start after >
                case States.AfterMore:
                    if (symbol == '>')
                        return new OperatorToken(_input.Lexeme, OperatorToken.Operation.BitwiseShiftRight, _input.LexemeLine, _input.LexemeColumn);
                    else if (symbol == '<')
                        return new OperatorToken(_input.Lexeme, OperatorToken.Operation.SymmetricDifference, _input.LexemeLine, _input.LexemeColumn);
                    else if (symbol == '=')
                        return new OperatorToken(_input.Lexeme, OperatorToken.Operation.MoreOreEqual, _input.LexemeLine, _input.LexemeColumn);
                    else {
                        _input.Retract();
                        return new OperatorToken(_input.Lexeme, OperatorToken.Operation.More, _input.LexemeLine, _input.LexemeColumn);
                    }
                //starts after *
                case States.AfterStar:
                    if (symbol == '*')
                        return new OperatorToken(_input.Lexeme, OperatorToken.Operation.Exponential, _input.LexemeLine, _input.LexemeColumn);
                    else if (symbol == '=')
                        return new OperatorToken(_input.Lexeme, OperatorToken.Operation.MultiplyAssign, _input.LexemeLine, _input.LexemeColumn);
                    else {
                        _input.Retract();
                        return new OperatorToken(_input.Lexeme, OperatorToken.Operation.Multiply, _input.LexemeLine, _input.LexemeColumn);
                    }
                //starts after :
                case States.AfterColon:
                    if (symbol == '=')
                        return new OperatorToken(_input.Lexeme, OperatorToken.Operation.Assign, _input.LexemeLine, _input.LexemeColumn);
                    else {
                        _input.Retract();
                        return new SeparatorToken(_input.Lexeme, SeparatorToken.Separator.Colon, _input.LexemeLine, _input.LexemeColumn);
                    }
                //starts after +
                case States.AfterPlus:
                    if (symbol == '=')
                        return new OperatorToken(_input.Lexeme, OperatorToken.Operation.PlusAssign, _input.LexemeLine, _input.LexemeColumn);
                    else {
                        _input.Retract();
                        return new OperatorToken(_input.Lexeme, OperatorToken.Operation.Plus, _input.LexemeLine, _input.LexemeColumn);
                    }
                //start after -
                case States.AfterMinus:
                    if (symbol == '=')
                        return new OperatorToken(_input.Lexeme, OperatorToken.Operation.MinusAssign, _input.LexemeLine, _input.LexemeColumn);
                    else {
                        _input.Retract();
                        return new OperatorToken(_input.Lexeme, OperatorToken.Operation.Minus, _input.LexemeLine, _input.LexemeColumn);
                    }
                //start after /
                case States.AfterSlash:
                    if (symbol == '/') {
                        _input.SkipLine();
                        currState = States.Start;
                    }
                    else if (symbol == '=')
                        return new OperatorToken(_input.Lexeme, OperatorToken.Operation.DivideAssign, _input.LexemeLine, _input.LexemeColumn);
                    else {
                        _input.Retract();
                        return new OperatorToken(_input.Lexeme, OperatorToken.Operation.Divide, _input.LexemeLine, _input.LexemeColumn);
                    }
                    break;
                
                 case States.AfterDot:
                     if (symbol == ')')
                         return new OperatorToken(_input.Lexeme, OperatorToken.Operation.CloseParenthesisWithDot, _input.LexemeLine, _input.LexemeColumn);
                     else {
                         _input.Retract();
                         return new OperatorToken(_input.Lexeme, OperatorToken.Operation.Dot, _input.LexemeLine, _input.LexemeColumn);
                     }
                
                case States.AfterParenthesis:
                    if (symbol == '*') {
                        ParenthesesComments.Parse(_input);
                        currState = States.Start;
                    }
                    else if (symbol == '.')
                        return new OperatorToken(_input.Lexeme, OperatorToken.Operation.OpenParenthesisWithDot, _input.LexemeLine, _input.LexemeColumn);
                    else {
                        _input.Retract();
                        return new OperatorToken(_input.Lexeme, OperatorToken.Operation.OpenParenthesis, _input.LexemeLine, _input.LexemeColumn);
                    }
                    
                    break;
                    // -- END OF States.AfterParenthesis
                
                case States.AfterAmpersand:
                    if (Symbols.octDigits.Contains((char) symbol))
                        return OctNumberAutomata.Parse(_input);
                    else
                        throw new UnknownLexemeException(_input.Lexeme, _input.LexemeLine, _input.LexemeColumn);
                    // -- END OF States.AfterParenthesis
                default:
                    throw new ArgumentOutOfRangeException();
            }                
        }
    }
}

// Start position after decimal digit [0-9]->[...]
public static class DecimalNumberAutomata {
    private enum States {BeforeDot, AfterDot, AfterExponentSign, AfterExponentAfterSign, Fraction}
    
    public static Token Parse(InputBuffer input) {
        var currState = States.BeforeDot;
        
        while (true) {
            var symbol = input.Read();

            switch (currState) {
                case States.BeforeDot:
                    if (symbol == '.')
                        currState = States.AfterDot;
                    else if (symbol == 'e' || symbol == 'E')
                        currState = States.AfterExponentSign;
                    else if (!Symbols.decDigits.Contains((char) symbol)) {
                        input.Retract();
                        return new IntegerToken(input.Lexeme, input.LexemeLine, input.LexemeColumn);
                    }
                    break;
                
                case States.AfterDot:
                    if (symbol == 'e' || symbol == 'E')
                        currState = States.AfterExponentSign;
                    else if (Symbols.decDigits.Contains((char) symbol)) {
                        currState = States.Fraction;
                    }
                    else {
                        input.Retract();
                        return new RealToken(input.Lexeme, input.LexemeLine, input.LexemeColumn);
                    }
                    
                    break;
                
                //start at [0-9].[0-9]->[...]
                case States.Fraction:
                    if (symbol == 'e' || symbol == 'E')
                        currState = States.AfterExponentSign;
                    
                    else if (!Symbols.decDigits.Contains((char) symbol)) {
                        input.Retract();
                        return new RealToken(input.Lexeme, input.LexemeLine, input.LexemeColumn);
                    }
                    
                    break;
                //start after E [0-9].[0-9][eE]->[...]
                case States.AfterExponentSign:
                    if (symbol == '+' || symbol == '-' || Symbols.decDigits.Contains((char) symbol))
                        currState = States.AfterExponentAfterSign;
                    else 
                        throw new UnknownLexemeException(input.Lexeme, input.LexemeLine, input.LexemeColumn);
                    break;
                
                //start after optional exponent sign [0-9].[0-9][eE][+-]?->[...]
                case States.AfterExponentAfterSign:
                    if (!Symbols.decDigits.Contains((char) symbol)) {
                        input.Retract();
                        return new RealToken(input.Lexeme, input.LexemeLine, input.LexemeColumn);
                    }
                    break;
            }
        }
    }
}

// Start position after $ $->[...]
public static class HexNumberAutomata {
    private enum States {AfterDollar, HexSequence}
    
    public static Token Parse(InputBuffer input) {
        var currState = States.AfterDollar;
        
        while (true) {
            var symbol = input.Read();

            switch (currState) {
                case States.AfterDollar:
                    if (Symbols.hexDigits.Contains((char) symbol))
                        currState = States.HexSequence;
                    else 
                        throw new UnknownLexemeException(input.Lexeme, input.LexemeLine, input.LexemeColumn);
                    break;
                case States.HexSequence:
                    if (!Symbols.hexDigits.Contains((char) symbol)) {
                        input.Retract();
                        return new IntegerToken(input.Lexeme, input.LexemeLine, input.LexemeColumn);
                    }
                    break;
            }
        }
    }
}

// Start position &[0-7]->[...]
public static class OctNumberAutomata {
    public static Token Parse(InputBuffer input) {
        while (true) {
            var symbol = input.Read();
            
            if (Symbols.octDigits.Contains((char) symbol))
                continue;
            
            input.Retract();
            return new IntegerToken(input.Lexeme, input.LexemeLine, input.LexemeColumn);
        }
    }
}

// Start position after % %->[...]
public static class BinaryNumberAutomata {
    private enum States {AfterPercent, BinarySequence}
    
    public static Token Parse(InputBuffer input) {
        var currState = States.AfterPercent;
        
        while (true) {
            var symbol = input.Read();
            
            switch (currState) {
                    case States.AfterPercent:
                        if ((char) symbol == '0' || (char) symbol == '1')
                            currState = States.BinarySequence;
                        else
                            throw new UnknownLexemeException(input.Lexeme, input.LexemeLine, input.LexemeColumn);
                        break;
                    case States.BinarySequence:
                        if (!((char) symbol == '0' || (char) symbol == '1')) {
                            input.Retract();
                            return new IntegerToken(input.Lexeme, input.LexemeLine, input.LexemeColumn);
                        }
                        break;
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
            
            input.Retract();
            var textInfo = CultureInfo.InvariantCulture.TextInfo;
            if (Enum.TryParse(textInfo.ToTitleCase(input.Lexeme), true, out ReservedToken.Words reservedWord))        
                if (Enum.IsDefined(typeof(ReservedToken.Words), reservedWord))  
                    return new ReservedToken(input.Lexeme, reservedWord, input.LexemeLine, input.LexemeColumn);
            
            return new IdentityToken(input.Lexeme, input.LexemeLine, input.LexemeColumn);
        }
    }
}

// Start position after # or ': #->[...]; '-> 
public static class StringAutomata {
    public  enum States {QuotedString, AfterHash, AfterQMark, AfterDollar, HexControlSeq, 
        AfterAmpersand, OctControlSeq, AfterPercent, BinaryControlSeq, DecControlSeq}

    private static States currentState;

    public static Token Parse(InputBuffer input, States state) {
        currentState = state;
        
        while (true) {
            var symbol = input.Read();
            
            switch (currentState) {
                //starts after '
                case States.QuotedString:
                    if (symbol == '\r' || symbol == '\n' || symbol == -1)
                        throw new StringExceedsLineException(input.Lexeme, input.LexemeLine, input.LexemeColumn);
                    else if (symbol == '\'')
                        currentState = States.AfterQMark;
                    break;
                
                case States.AfterQMark:
                    if (symbol == '\'')
                        currentState = States.QuotedString;
                    else if (symbol == '#')
                        currentState = States.AfterHash;
                    else {
                        input.Retract();
                        return new StringToken(input.Lexeme, input.LexemeLine, input.LexemeColumn);
                    }
                    break;
                //start after #
                case States.AfterHash:
                    if (symbol == '$')
                        currentState = States.AfterDollar;
                    else if (symbol == '&')
                        currentState = States.AfterAmpersand;
                    else if (symbol == '%')
                        currentState = States.AfterPercent;
                    else if (Symbols.decDigits.Contains((char) symbol))
                        currentState = States.DecControlSeq;
                    else
                        throw new StringMalformedException(input.Lexeme, input.LexemeLine, input.LexemeColumn);
                        
                    break;
                //--- HEXADECIMAL CONTROL SEQUENCE ---
                //starts after $->[...]
                case States.AfterDollar:
                    if (Symbols.hexDigits.Contains((char) symbol))
                        currentState = States.HexControlSeq;
                    else
                        throw new StringMalformedException(input.Lexeme, input.LexemeLine, input.LexemeColumn);
                    break;
                //starts  $[0-7A-Fa-f]->[...]
                case States.HexControlSeq:
                    if (symbol == '\'')
                        currentState = States.QuotedString;
                    else if (symbol == '#')
                        currentState = States.AfterHash;
                    else if (!Symbols.hexDigits.Contains((char) symbol)) {
                        input.Retract();
                        return new StringToken(input.Lexeme, input.LexemeLine, input.LexemeColumn);
                    }      
                    break;
                //--- END HEXADECIMAL CONTROL SEQUENCE ---
                
                //--- OCTADEMICAL CONTROL SEQUENCE ---
                //starts after &
                case States.AfterAmpersand:
                    if (Symbols.octDigits.Contains((char) symbol))
                        currentState = States.OctControlSeq;
                    else
                        throw new StringMalformedException(input.Lexeme, input.LexemeLine, input.LexemeColumn);
                    break;
                //starts after &[0-8]->[...]
                case States.OctControlSeq:
                    if (symbol == '\'')
                        currentState = States.QuotedString;
                    else if (symbol == '#')
                        currentState = States.AfterHash;
                    else if (!Symbols.octDigits.Contains((char) symbol)) {
                        input.Retract();
                        return new StringToken(input.Lexeme, input.LexemeLine, input.LexemeColumn);
                    }      
                    break;
                //--- END OCTADEMICAL CONTROL SEQUENCE ---
                
                //--- BINARY CONTROL SEQUENCE ---
                //starts after %
                case States.AfterPercent:
                    if ((char) symbol == '1' || (char) symbol == '0')
                        currentState = States.BinaryControlSeq;
                    else
                        throw new StringMalformedException(input.Lexeme, input.LexemeLine, input.LexemeColumn);
                    break;
                
                case States.BinaryControlSeq:
                    if (symbol == '\'')
                        currentState = States.QuotedString;
                    else if (symbol == '#')
                        currentState = States.AfterHash;
                    else if (!((char) symbol == '1' || (char) symbol == '0')) {
                        input.Retract();
                        return new StringToken(input.Lexeme, input.LexemeLine, input.LexemeColumn);
                    }      
                    break;
                //--- END CONTROL SEQUENCE ---
                
                //--- DECIMAL CONTROL SEQUENCE ---
                //starts at #[0-9]->[...]
                case States.DecControlSeq:
                    if (symbol == '\'')
                        currentState = States.QuotedString;
                    else if (symbol == '#')
                        currentState = States.AfterHash;
                    else if (!Symbols.decDigits.Contains((char) symbol)) {
                        input.Retract();
                        return new StringToken(input.Lexeme, input.LexemeLine, input.LexemeColumn);
                    }      
                    break;
                //--- END DECIMAL CONTROL SEQUENCE ---
            }            
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
                    throw new UnclosedCommentException(input.Lexeme, input.LexemeLine, input.LexemeColumn);
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
                        throw new UnclosedCommentException(input.Lexeme, input.LexemeLine, input.LexemeColumn);
                    }
                    else if (symbol == '*')
                        currState = States.AfterAsterisk;
                    break;
                
                case States.AfterAsterisk:
                    if (symbol == Symbols.EOF) {
                        throw new UnclosedCommentException(input.Lexeme, input.LexemeLine, input.LexemeColumn);
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
