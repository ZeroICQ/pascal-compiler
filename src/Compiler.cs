using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

[assembly:InternalsVisibleTo("Tests")]


namespace Compiler {

internal class Compiler {
    private LexemesAutomata _lexer;
    
    public Compiler(in TextReader input) {
        _lexer = new LexemesAutomata(new InputBuffer(input));
    }
    
    public Token GetNextToken() {
        return _lexer.Parse();
    }
}

internal static class App {
    private static void ShowUsage() {
        Console.WriteLine("[USAGE]");
        Console.WriteLine("dotnet compiler.dll [OPTIONS] source.pas");
        Console.WriteLine("");
        Console.WriteLine("[OPTIONS]");
        Console.WriteLine("-l                perform only lexical analyze");
        Console.WriteLine("-o filename       output file");
    }
    
    public static int  Main(string[] args) {
        if (args.Length == 0) {
            ShowUsage();
            return 0;
        }

        var inputFilePath = args.Last();

        try {
            using (var fileStream = File.OpenText(inputFilePath)) {
                var compiler = new Compiler(fileStream);
                
                Token lt = null;
                
                do {
                    try {
                        lt = compiler.GetNextToken();
                        Console.WriteLine("{0},{1}\t{2}\t{3}\t{4}", lt.Line.ToString(), lt.Column.ToString(),
                            lt.Type.ToString(), lt.StringValue, lt.Lexeme);
                    }
                    catch (LexerException ex) {
                        Console.WriteLine(ex.Message);
                    }
                } while (!(lt is EofToken));
            }
        }
        catch (FileNotFoundException ex) {
            Console.WriteLine($"{ex.FileName} not found.");
        }
        
//        var compiler = new Compiler(new StreamReader(Console.OpenStandardInput()));
        
//            TODO: parse flags
//            var outputFilenameIndex = Array.FindIndex(args, s => s.Equals("-o"));
        return 0;
    }
}
}
