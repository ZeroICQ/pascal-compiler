﻿using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

[assembly:InternalsVisibleTo("Tests")]


namespace Compiler {

internal class Compiler : IDisposable {
    private StreamReader _input;
    private LexemesAutomata _lexer;
    
    public Compiler(StreamReader input) {
        _input = input;
        _lexer = new LexemesAutomata(input);
    }
    
    public Token GetNextToken() {
        return _lexer.Parse();
    }

    public void Dispose() {
        _input?.Dispose();
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
            using (var compiler = new Compiler(File.OpenText(inputFilePath))) {
                try {
                    Token lt;
                    while ((lt = compiler.GetNextToken()) != null) {
                        Console.WriteLine("{0},{1}\t{2}\t{3}\t{4}", lt.Line.ToString(), lt.Column.ToString(),
                            lt.Type.ToString(), lt.GetStringValue(), lt.ToString());
                    }
                }
                catch (UnkownLexemeException ex) {
                    Console.WriteLine($"Unknown lexeme at {ex.Line.ToString()}, {ex.Line.ToString()}");
                }
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
