using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

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
            var visitorTree = ast.Accept(new PrintVisitor());
            var canvas = new List<StringBuilder>();
            visitorTree.Print(canvas);

            foreach (var line in canvas) {
                Console.WriteLine(line);
            }
        }
        catch (ParsingException e) {
            Console.WriteLine(e.Message);
        }
    }
}
}
