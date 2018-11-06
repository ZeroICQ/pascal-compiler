using System;
using System.Collections.Generic;
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
    private enum  Options { OnlyLexical, OnlySyntax }
    private static HashSet<Options> enabledOptions = new HashSet<Options>();
    
    public static int  Main(string[] args) {
        if (args.Length == 0) {
            ShowUsage();
            return 0;
        }
        
        ParseOptions(args);

        var inputFilePath = args.Last();


        StreamReader input = null;

        if (!inputFilePath.StartsWith('-')) {
            try {
                input = File.OpenText(inputFilePath);
            }
            catch (FileNotFoundException ex) {
                Console.WriteLine($"{ex.FileName} not found.");
            }
            catch (Exception e) {
                Console.WriteLine(e);
                throw;
            }

        }
        else {
            input = new StreamReader(Console.OpenStandardInput());
        }

                
        if (enabledOptions.Contains(Options.OnlyLexical)) {
            PerformLexicalAnalysis(input);
        }

//        var compiler = new Compiler(new StreamReader(Console.OpenStandardInput()));
        
//            TODO: parse flags
//            var outputFilenameIndex = Array.FindIndex(args, s => s.Equals("-o"));
        return 0;
    }

    private static void PerformLexicalAnalysis(StreamReader streamReader) {
        var compiler = new Compiler(streamReader);
            
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

    private static void ParseOptions(string[] args) {
        foreach (var opt in args) {
            if (opt == "-l") {
                enabledOptions.Add(Options.OnlyLexical);
                if (enabledOptions.Contains(Options.OnlySyntax))
                    throw new ArgumentException();
            }
            
            if (opt == "-s") {
                enabledOptions.Add(Options.OnlySyntax);
                if (enabledOptions.Contains(Options.OnlyLexical))
                    throw new ArgumentException();
            }
        }
        
        
    }

    private static void ShowUsage() {
        Console.WriteLine("[USAGE]");
        Console.WriteLine("dotnet compiler.dll [OPTIONS] source.pas");
        Console.WriteLine("");
        Console.WriteLine("[OPTIONS]");
        Console.WriteLine("-l                perform only lexical analysis");
        Console.WriteLine("-s                perform only syntax  analysis");
        Console.WriteLine("-o filename       output file [WIP]");
    }
}
}
