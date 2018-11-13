using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace Compiler {

internal static class Symbols {
    public static readonly HashSet<char> decDigits = new HashSet<char>("0123456789".ToCharArray());
    public static readonly HashSet<char> octDigits = new HashSet<char>("01234567".ToCharArray());
    public static readonly HashSet<char> hexDigits = new HashSet<char>("0123456789ABCDEFabcdef".ToCharArray());
    public static readonly HashSet<char> letters = new HashSet<char>("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz".ToCharArray());
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

    private bool _isRetracted = false;
    private Token lastToke
    public Token GetNextToken() {
        var currState = States.Start;
        
        while (true) {
            //must be performed before Read(), due to whitespace skipping in startLexeme()
            if (currState == States.Start)
                _input.StartLexeme();
            
            var symbol = _input.Read();
            
            switch (currState) {
                case States.Start:

                    switch (symbol) {
                        case Symbols.EOF:
                            return TokenFactory.Build<EofToken>(_input);
                        case '/':
                            currState = States.AfterSlash;
                            break;
                        case '{':
                            BracesCommentAutomata.Parse(_input);
                            break;
                        case '(':
                            currState = States.AfterParenthesis;
                            break;
                        case '$':
                            return HexNumberAutomata.Parse(_input);
                        case '&':
                            currState = States.AfterAmpersand;
                            break;
                        case '%':
                            return BinaryNumberAutomata.Parse(_input);
                        case '\'':
                            return StringAutomata.Parse(_input, StringAutomata.States.QuotedString);
                        case '#':
                            return StringAutomata.Parse(_input, StringAutomata.States.AfterHash);
                        case '+':
                            currState = States.AfterPlus;
                            break;
                        case '-':
                            currState = States.AfterMinus;
                            break;
                        case '*':
                            currState = States.AfterStar;
                            break;
                        case '=':
                            return TokenFactory.BuildOperator(_input, OperatorToken.Operation.Equal);
                        case '<':
                            currState = States.AfterLess;
                            break;
                        case '>':
                            currState = States.AfterMore;
                            break;
                        case '[':
                            return TokenFactory.BuildOperator(_input, OperatorToken.Operation.OpenBracket);
                        case ']':
                            return TokenFactory.BuildOperator(_input, OperatorToken.Operation.CloseBracket);
                        case '.':
                            currState = States.AfterDot;
                            break;
                        case ')':
                            return TokenFactory.BuildOperator(_input, OperatorToken.Operation.CloseParenthesis);
                        case '^':
                            return TokenFactory.BuildOperator(_input, OperatorToken.Operation.Caret);
                        case '@':
                            return TokenFactory.BuildOperator(_input, OperatorToken.Operation.AtSign);
                        case ';':
                            return TokenFactory.BuildSeparator(_input, SeparatorToken.Separator.Semicolon);
                        case ',':
                            return TokenFactory.BuildSeparator(_input, SeparatorToken.Separator.Comma);
                        case ':':
                            currState = States.AfterColon;
                            break;
                        default: 
                            if (Symbols.letters.Contains((char) symbol) || symbol == '_')
                                return IdentityAutomata.Parse(_input);
                            else if (Symbols.decDigits.Contains((char) symbol)) {
                                return DecimalNumberAutomata.Parse(_input);
                            }
                            else
                                throw new UnknownLexemeException(_input.Lexeme, _input.LexemeLine, _input.LexemeColumn);
                    }
                    break;
                    // --- END OF States.Start ---
                    
                //start after  <
                case States.AfterLess:
                    switch (symbol) {
                        case '>':
                            return TokenFactory.BuildOperator(_input, OperatorToken.Operation.NotEqual);
                        case '<':
                            return TokenFactory.BuildOperator(_input, OperatorToken.Operation.BitwiseShiftLeft);
                        case '=':
                            return TokenFactory.BuildOperator(_input, OperatorToken.Operation.LessOrEqual);
                        default:
                            _input.Retract();
                            return TokenFactory.BuildOperator(_input, OperatorToken.Operation.Less);
                    }
                //start after >
                case States.AfterMore:
                    switch (symbol) {
                        case '>':
                            return TokenFactory.BuildOperator(_input, OperatorToken.Operation.BitwiseShiftRight);
                        case '<':
                            return TokenFactory.BuildOperator(_input, OperatorToken.Operation.SymmetricDifference);
                        case '=':
                            return TokenFactory.BuildOperator(_input, OperatorToken.Operation.MoreOreEqual);
                        default:
                            _input.Retract();
                            return TokenFactory.BuildOperator(_input, OperatorToken.Operation.More);
                    }
                //starts after *
                case States.AfterStar:
                    switch (symbol) {
                        case '*':
                            return TokenFactory.BuildOperator(_input, OperatorToken.Operation.Exponential);
                        case '=':
                            return TokenFactory.BuildOperator(_input, OperatorToken.Operation.MultiplyAssign);
                        default:
                            _input.Retract();
                            return TokenFactory.BuildOperator(_input, OperatorToken.Operation.Multiply);
                    }
                //starts after :
                case States.AfterColon:
                    if (symbol == '=')
                        return TokenFactory.BuildOperator(_input, OperatorToken.Operation.Assign);
                    else {
                        _input.Retract();
                        return TokenFactory.BuildSeparator(_input, SeparatorToken.Separator.Colon);
                    }
                //starts after +
                case States.AfterPlus:
                    if (symbol == '=')
                        return TokenFactory.BuildOperator(_input, OperatorToken.Operation.PlusAssign);
                    else {
                        _input.Retract();
                        return TokenFactory.BuildOperator(_input, OperatorToken.Operation.Plus);
                    }
                //start after -
                case States.AfterMinus:
                    if (symbol == '=')
                        return TokenFactory.BuildOperator(_input,OperatorToken.Operation.MinusAssign);
                    else {
                        _input.Retract();
                        return TokenFactory.BuildOperator(_input, OperatorToken.Operation.Minus);
                    }
                //start after /
                case States.AfterSlash:
                    switch (symbol) {
                        case '/':
                            _input.SkipLine();
                            currState = States.Start;
                            break;
                        case '=':
                            return TokenFactory.BuildOperator(_input, OperatorToken.Operation.DivideAssign); 
                        default:
                            _input.Retract();
                            return TokenFactory.BuildOperator(_input, OperatorToken.Operation.Divide);
                    }
                    break;
                
                case States.AfterDot:
                    if (symbol == ')')
                        return TokenFactory.BuildOperator(_input, OperatorToken.Operation.CloseParenthesisWithDot);
                    else {
                        _input.Retract();
                        return TokenFactory.BuildOperator(_input, OperatorToken.Operation.Dot);
                    }
                
                case States.AfterParenthesis:
                    switch (symbol) {
                        case '*':
                            ParenthesesComments.Parse(_input);
                            currState = States.Start;
                            break;
                        case '.':
                            return TokenFactory.BuildOperator(_input, OperatorToken.Operation.OpenParenthesisWithDot);
                        default:
                            _input.Retract();
                            return TokenFactory.BuildOperator(_input, OperatorToken.Operation.OpenParenthesis);
                    }
                    break;
                    // -- END OF States.AfterParenthesis
                
                case States.AfterAmpersand:
                    if (Symbols.octDigits.Contains((char) symbol))
                        return OctNumberAutomata.Parse(_input);
                    else if (Symbols.letters.Contains((char) symbol) || symbol == '_')
                        return IdentityAutomata.Parse(_input);
                    else
                        throw new UnknownLexemeException(_input.Lexeme, _input.LexemeLine, _input.LexemeColumn);
                    // -- END OF States.AfterParenthesis
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
                    switch (symbol) {
                        case '.':
                            currState = States.AfterDot;
                            break;
                        case 'e':
                        case 'E':
                            currState = States.AfterExponentSign;
                            break;
                        default: {
                            if (!Symbols.decDigits.Contains((char) symbol)) {
                                input.Retract();
                                return TokenFactory.Build<IntegerToken>(input);
                            }
                            break;
                        }
                    }
                    break;
                
                case States.AfterDot:
                    switch (symbol) {
                        case 'e':
                        case 'E':
                            currState = States.AfterExponentSign;
                            break;
                        default: {
                            if (Symbols.decDigits.Contains((char) symbol)) {
                                currState = States.Fraction;
                            }
                            else {
                                input.Retract();
                                return TokenFactory.Build<RealToken>(input);
                            }
                            break;
                        }
                    }
                    
                    break;
                
                //start at [0-9].[0-9]->[...]
                case States.Fraction:
                    switch (symbol) {
                        case 'e':
                        case 'E':
                            currState = States.AfterExponentSign;
                            break;
                        default: {
                            if (!Symbols.decDigits.Contains((char) symbol)) {
                                input.Retract();
                                return TokenFactory.Build<RealToken>(input);
                            }

                            break;
                        }
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
                        return TokenFactory.Build<RealToken>(input);
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
                        return TokenFactory.Build<IntegerToken>(input);
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
            return TokenFactory.Build<IntegerToken>(input);
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
                            return TokenFactory.Build<IntegerToken>(input);
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
                    return TokenFactory.BuildReserved(input, reservedWord);
            
            return TokenFactory.Build<IdentityToken>(input);
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
                    switch (symbol) {
                        case '\r':
                        case '\n':
                        case -1:
                            throw new StringExceedsLineException(input.Lexeme, input.LexemeLine, input.LexemeColumn);
                        case '\'':
                            currentState = States.AfterQMark;
                            break;
                    }
                    break;
                
                case States.AfterQMark:
                    switch (symbol) {
                        case '\'':
                            currentState = States.QuotedString;
                            break;
                        case '#':
                            currentState = States.AfterHash;
                            break;
                        default:
                            input.Retract();
                            return TokenFactory.Build<StringToken>(input); 
                    }
                    break;
                //start after #
                case States.AfterHash:
                    switch (symbol) {
                        case '$':
                            currentState = States.AfterDollar;
                            break;
                        case '&':
                            currentState = States.AfterAmpersand;
                            break;
                        case '%':
                            currentState = States.AfterPercent;
                            break;
                        default: {
                            if (Symbols.decDigits.Contains((char) symbol))
                                currentState = States.DecControlSeq;
                            else
                                throw new StringMalformedException(input.Lexeme, input.LexemeLine, input.LexemeColumn);
                            break;
                        }
                    }
                        
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
                    switch (symbol) {
                        case '\'':
                            currentState = States.QuotedString;
                            break;
                        case '#':
                            currentState = States.AfterHash;
                            break;
                        default: {
                            if (!Symbols.hexDigits.Contains((char) symbol)) {
                                input.Retract();
                                return TokenFactory.Build<StringToken>(input);
                            }
                            break;
                        }
                    }      
                    break;
                //--- END HEXADECIMAL CONTROL SEQUENCE ---
                
                //--- OCTAL CONTROL SEQUENCE ---
                //starts after &
                case States.AfterAmpersand:
                    if (Symbols.octDigits.Contains((char) symbol))
                        currentState = States.OctControlSeq;
                    else
                        throw new StringMalformedException(input.Lexeme, input.LexemeLine, input.LexemeColumn);
                    break;
                //starts after &[0-8]->[...]
                case States.OctControlSeq:
                    switch (symbol) {
                        case '\'':
                            currentState = States.QuotedString;
                            break;
                        case '#':
                            currentState = States.AfterHash;
                            break;
                        default: {
                            if (!Symbols.octDigits.Contains((char) symbol)) {
                                input.Retract();
                                return TokenFactory.Build<StringToken>(input);
                            }

                            break;
                        }
                    }      
                    break;
                //--- END OCTAL CONTROL SEQUENCE ---
                
                //--- BINARY CONTROL SEQUENCE ---
                //starts after %
                case States.AfterPercent:
                    if ((char) symbol == '1' || (char) symbol == '0')
                        currentState = States.BinaryControlSeq;
                    else
                        throw new StringMalformedException(input.Lexeme, input.LexemeLine, input.LexemeColumn);
                    break;
                
                case States.BinaryControlSeq:
                    switch (symbol) {
                        case '\'':
                            currentState = States.QuotedString;
                            break;
                        case '#':
                            currentState = States.AfterHash;
                            break;
                        default: {
                            if (!((char) symbol == '1' || (char) symbol == '0')) {
                                input.Retract();
                                return TokenFactory.Build<StringToken>(input);
                            }
                            break;
                        }
                    }      
                    break;
                //--- END CONTROL SEQUENCE ---
                
                //--- DECIMAL CONTROL SEQUENCE ---
                //starts at #[0-9]->[...]
                case States.DecControlSeq:
                    switch (symbol) {
                        case '\'':
                            currentState = States.QuotedString;
                            break;
                        case '#':
                            currentState = States.AfterHash;
                            break;
                        default: {
                            if (!Symbols.decDigits.Contains((char) symbol)) {
                                input.Retract();
                                return TokenFactory.Build<StringToken>(input);
                            }
                            break;
                        }
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


public static class TokenFactory {
    public static Token Build<T>(InputBuffer input) where T : Token {
        try {
            try {
                return (T)Activator.CreateInstance(typeof(T), input.Lexeme, input.LexemeLine, input.LexemeColumn);
            }
            catch (MissingMethodException) {
                return (T)Activator.CreateInstance(typeof(T), input.LexemeLine, input.LexemeColumn);
            }
        }
        catch (TargetInvocationException e) {
            throw e.InnerException;
        }
        
        
    }

    public static OperatorToken BuildOperator(InputBuffer input, OperatorToken.Operation op) {
        return new OperatorToken(input.Lexeme, op, input.LexemeLine, input.LexemeColumn);
    }
    
    public static SeparatorToken BuildSeparator(InputBuffer input, SeparatorToken.Separator sep) {
        return new SeparatorToken(input.Lexeme, sep, input.LexemeLine, input.LexemeColumn);
    }

    public static ReservedToken BuildReserved(InputBuffer input, ReservedToken.Words word) {
        return new ReservedToken(input.Lexeme, word, input.LexemeLine, input.LexemeColumn);
    }
}
} //namespace Compiler  
