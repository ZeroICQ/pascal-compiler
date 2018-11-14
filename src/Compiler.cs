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
        try {
            var ast = GetAst();
        }
        catch (ParsingException e) {
            Console.WriteLine(e.Message);
        }
        
//        ast.Accept(new AstPrinter());
    }
}

}
