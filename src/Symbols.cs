using System.Collections.Generic;

namespace Compiler {
public static class Symbols {
    public static readonly HashSet<char> decDigits = new HashSet<char>("0123456789".ToCharArray());
    public static readonly HashSet<char> octDigits = new HashSet<char>("01234567".ToCharArray());
    public static readonly HashSet<char> hexDigits = new HashSet<char>("0123456789ABCDEFabcdef".ToCharArray());
    public static readonly HashSet<char> letters = new HashSet<char>("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz".ToCharArray());
    public const int EOF = -1;

    public enum Separators {
        Comma,     // ,
        Colon,     // :
        Semicolon  // ; 
    }

    public enum Words {
        Absolute, And, Array, Asm, Begin, Case, Const, Constructor, Destructor, Div, Do, Downto, Else, End, File, For,
        Function, Goto, If, Implementation, In, Inherited, Inline, Interface, Label, Mod, Nil, Not, Object, Of, Operator,
        Or, Packed, Procedure, Program, Record, Reintroduce, Repeat, Self, Set, Shl, Shr, String, Then, To, Type, Unit,
        Until, Uses, Var, While, With, Xor
    }

    public enum Operation {
        Plus,                   // +
        Minus,                  // -
        Multiply,               // *
        Divide,                 // /
        Equal,                  // =
        Less,                   // <
        More,                   // >
        OpenBracket,            // [
        CloseBracket,           // ]
        Dot,                    // .
        OpenParenthesis,        // (
        CloseParenthesis,       // )    
        Caret,                  // ^
        AtSign,                 // @
        NotEqual,               // <>
        BitwiseShiftLeft,       // <<
        BitwiseShiftRight,      // >>
        Exponential,            // **
        SymmetricDifference,    // ><
        LessOrEqual,            // <=
        MoreOreEqual,           // >=
        Assign,                 // :=
        PlusAssign,             // +=
        MinusAssign,            // -=
        MultiplyAssign,         // *=
        DivideAssign,           // /=
        OpenParenthesisWithDot, // (.
        CloseParenthesisWithDot // .)
    }
}
}
