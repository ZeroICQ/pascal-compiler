using System;
using System.IO;
using CommandLineParser.Arguments;
using CommandLineParser.Exceptions;
using CommandLineParser.Validation;

namespace Compiler {

internal static class App {
    public static int  Main(string[] args) {
        var commandLineParser = new CommandLineParser.CommandLineParser();
        
        var sourcePath = new FileArgument('i', "input", "Source file path") { Optional = false };
        var lexicalAnalysis = new SwitchArgument('l', "lexical", false);
        var syntaxAnalysis = new SwitchArgument('s', "syntax", false);
        
        commandLineParser.Arguments.Add(sourcePath);
        commandLineParser.Arguments.Add(lexicalAnalysis);
        commandLineParser.Arguments.Add(syntaxAnalysis);

        var compileStageGroupCertification = new ArgumentGroupCertification("l,s", EArgumentGroupCondition.ExactlyOneUsed);
        commandLineParser.Certifications.Add(compileStageGroupCertification);

        try {
            commandLineParser.ParseCommandLine(args);
        }
        catch (CommandLineException) {
            commandLineParser.ShowUsage();
            return 1;
        }

        using (var input = new StreamReader(sourcePath.OpenFileRead())) {
            if (lexicalAnalysis.Value)
                PerformLexicalAnalysis(input);
            
            if (syntaxAnalysis.Value)
                PerformSyntaxAnalysis(input);
        }
            
        return 0;
    }

    private static void PerformSyntaxAnalysis(StreamReader streamReader) {
        var compiler = new Compiler(streamReader);
        compiler.PrintAst();
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
}
}
