using System;
using System.IO;
using System.Runtime.CompilerServices;

[assembly:InternalsVisibleTo("Tests")]


namespace Compiler {

internal class Compiler {
    private LexemesAutomata _lexer;
    
    public Compiler(in TextReader input) {
        _lexer = new LexemesAutomata(new InputBuffer(input));
    }
    
    public Token GetNextToken() {
        return _lexer.GetNextToken();
    }

    public AstNode GetAst() {
        return new Parser(_lexer).GetAst();
    }

    public void PrintAst() {
        var ast = GetAst();
        ast.Accept(new AstPrinter());
    }
}

}
