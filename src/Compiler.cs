using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

[assembly:InternalsVisibleTo("Tests")]


namespace Compiler {

internal class Compiler {
    private LexemesAutomata _lexer;
    private bool _checkSemantics;
    
    public Compiler(in TextReader input, bool checkSemantics) {
        _lexer = new LexemesAutomata(new InputBuffer(input));
        _checkSemantics = checkSemantics;
    }
    
    public Token GetNextToken() {
        return _lexer.GetNextToken();
    }

    public AstNode Parse() {
        return new Parser(_lexer, _checkSemantics).Parse();
    }

    public void PrintAst() {
        try {
            var ast = Parse();
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
