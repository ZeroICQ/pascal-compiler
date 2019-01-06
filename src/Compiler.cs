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

    public (AstNode, SymStack) Parse() {
        return new Parser(_lexer, _checkSemantics).Parse();
    }
    

    public void PrintAst() {
        try {
            var (ast, symStack) = Parse();
            var visitorTree = ast.Accept(new PrintVisitor());
            var canvas = new List<StringBuilder>();
            visitorTree.Print(canvas);

            if (!_checkSemantics) {
                foreach (var line in canvas) {
                    Console.WriteLine(line);
                }
                return;
            }

            var symbolPrinter = new SymbolPrinterVisitor();
            foreach (var table in symStack) {
                foreach (var symbol in table) {
                    symbol.Accept(symbolPrinter);                    
                }
            }
            
            symbolPrinter.Print(canvas);
            foreach (var line in canvas) {
                Console.WriteLine(line);
            } 
        }
        catch (ParsingException e) {
            Console.WriteLine(e.Message);
        }
    }

    public void printAsm() {
        try {
            var (ast, symStack) = Parse();
            var output = Console.Out;
            var asmVisitor = new AsmVisitor(output, ast, symStack);
            asmVisitor.Generate();
        }
        catch (ParsingException e) {
            Console.WriteLine(e.Message);
        }
    }
}
}
